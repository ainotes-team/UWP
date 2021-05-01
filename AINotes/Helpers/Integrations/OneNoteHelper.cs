using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using AINotes.Components.Implementations;
using AINotes.Models;
using ExCSS;
using Helpers;
using Helpers.Extensions;
using HtmlAgilityPack;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Logger = Helpers.Logger;
using LogLevel = Helpers.LogLevel;

namespace AINotes.Helpers.Integrations {
    // TODO: Better Conversion
    //      text: div width -> linebreak
    //      text: some formatting properties (font, font color, ...)
    //      lists: handle ordered list separately (ol) 
    //      lists: fix weird behavior
    //      tables: rowWidth -> Match Text / OneNote
    //      tables: text formatting in tables
    //      math
    public static class OneNoteHelper {
        private static readonly IPublicClientApplication Pca;
        private static GraphServiceClient _graphClient;
        
        private static readonly StylesheetParser CssParser = new StylesheetParser();

        private const string ClientId = Configuration.LicenseKeys.MicrosoftGraph;
        private static readonly string[] AppScopes = {"Notes.read", "Notes.ReadWrite", "Notes.ReadWrite.All"};

        static OneNoteHelper() {
            Pca = PublicClientApplicationBuilder.Create(ClientId).WithRedirectUri($"msal{ClientId}://auth").Build();
        }
        
        public static async Task Login() {
            var accounts = await Pca.GetAccountsAsync();
            var account = accounts.FirstOrDefault();

            AuthenticationResult result = null;
            try {
                result = await Pca.AcquireTokenSilent(AppScopes, account).ExecuteAsync();
                Logger.Log("Token:", result.AccessToken.Length);
            } catch {
                Logger.Log("AcquireTokenSilent failed");
            }

            if (result?.AccessToken == null) {
                Logger.Log("AcquireTokenInteractive");
                result = await Pca.AcquireTokenInteractive(AppScopes).WithAccount(account).ExecuteAsync();
                Logger.Log("Token:", result.AccessToken.Length);
            }
            
            _graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(requestMessage => {
                requestMessage.Headers.TryAddWithoutValidation("Authorization", "Bearer " + result.AccessToken);
                return Task.FromResult(0);
            }));
        }

        public static async Task<List<Notebook>> GetNotebooks() {
            var currentNotebooks = await _graphClient.Me.Onenote.Notebooks.Request().Expand("sections").GetAsync();
            var allNotebooks = currentNotebooks.Select(s => s).ToList();

            while (currentNotebooks.NextPageRequest != null) {
                currentNotebooks = await currentNotebooks.NextPageRequest.Expand("sections").GetAsync();
                allNotebooks.AddRange(currentNotebooks.Select(s => s));
            }

            return allNotebooks;
        }

        public static async Task<List<OnenoteSection>> GetSections(Notebook notebook) {
            var currentSections = await new NotebookSectionsCollectionRequestBuilder(notebook.SectionsUrl, _graphClient).Request().GetAsync();
            var allSections = currentSections.Select(s => s).ToList();

            while (currentSections.NextPageRequest != null) {
                currentSections = await currentSections.NextPageRequest.GetAsync();
                allSections.AddRange(currentSections.Select(s => s));
            }

            return allSections;
        }

        public static async Task<List<OnenotePage>> GetPages(OnenoteSection section) {
            var currentPages = await new OnenotePagesCollectionRequestBuilder(section.PagesUrl, _graphClient).Request().GetAsync();
            var allPages = currentPages.Select(s => s).ToList();

            while (currentPages.NextPageRequest != null) {
                currentPages = await currentPages.NextPageRequest.GetAsync();
                allPages.AddRange(currentPages.Select(s => s));
            }

            return allPages;
        }

        public static async Task<string> GetPageContent(OnenotePage page) {
            var pageContentStream = await new OnenotePageContentRequestBuilder(page.ContentUrl + "?includeinkML=true", _graphClient).Request().GetAsync();
            var reader = new StreamReader(pageContentStream);
            var pageContent = reader.ReadToEnd();
            reader.Dispose();

            return pageContent;
        }

        public class ParseResult {
            public readonly string Title;
            public readonly List<ComponentModel> Components;
            public readonly List<StrokeModel> Strokes;

            public ParseResult(string title, List<ComponentModel> components, List<StrokeModel> strokes) {
                Title = title;
                Components = components;
                Strokes = strokes;
            }
        }

        private enum CtType {
            Text,
            RawRtf
        }
        
        private static async Task<List<ComponentModel>> ParsePageChild(HtmlNode containerChild) {
            var styleString = containerChild.GetAttributeValue("style", "");
            var style = (StyleRule) CssParser.Parse(".x{ " + styleString + " }").StyleRules.First();
            float x, y, width;
            switch (containerChild.Name) {
                case "div":
                    // default containers
                    float.TryParse(style.Style.Left.Replace("px", "").Replace("pt", ""), out x);
                    float.TryParse(style.Style.Top.Replace("px", "").Replace("pt", ""), out y);
                    float.TryParse(style.Style.Width.Replace("px", "").Replace("pt", ""), out width);

                    // Logger.Log($"Container | X: {x}, Y: {y}, W: {width}");
                    var components = new List<ComponentModel>();
                    var currentTextContent = new List<(CtType, string, bool, bool, bool, double, bool)>();

                    string GetTextRtf(string line, bool bold, bool italic, bool underline, double fontSize, bool linebreak) {
                        var modifiers = $"{(bold ? @"\b" : "")}{(italic ? @"\i" : "")}{(underline ? @"\ul" : "")}";
                        var modifiers0 = $"{(bold ? @"\b0" : "")}{(italic ? @"\i0" : "")}{(underline ? @"\ulnone" : "")}";
                        
                        return $@"\pard\tx720\cf1\f0{modifiers}\fs{fontSize * 2} " + line + $@"{modifiers0}{(linebreak ? @"\par" : "")}";
                    }

                    void AddTextModel() {
                        // header
                        var content = @"{\rtf1\fbidis\ansi\ansicpg1252\deff0\nouicompat\deflang1031{\fonttbl{\f0\fnil\fcharset0 Segoe UI;}{\f1\fnil Segoe UI;}}";
                        content += "\n" + @"{\colortbl ;\red0\green0\blue0;}";
                        
                        // content
                        foreach (var (type, line, bold, italic, underline, fontSize, linebreak) in currentTextContent) {
                            switch (type) {
                                case CtType.Text:
                                    content += "\n" + GetTextRtf(System.Net.WebUtility.HtmlDecode(line), bold, italic, underline, fontSize, linebreak);
                                    break;
                                case CtType.RawRtf:
                                    content += "\n" + line;
                                    break;
                            }
                        }

                        // end
                        content += "\n" + @"}";
                        
                        components.Add(new ComponentModel {
                            Type = "TextComponent",
                            Content = content,
                            PosX = x,
                            PosY = y,
                            SizeX = 200,
                            SizeY = 200
                        });
                    }
                    
                    foreach (var divChild in containerChild.ChildNodes) {
                        if (divChild.NodeType != HtmlNodeType.Element) continue;
                        switch (divChild.Name) {
                            case "img":
                                if (currentTextContent.Count > 0) {
                                    // add text model
                                    AddTextModel();
                                    currentTextContent.Clear();
                                }
                                components.AddRange(await ParsePageChild(divChild));
                                break;
                            case "h1":
                                foreach (var textPart in divChild.InnerText.Split('￼')) {
                                    currentTextContent.Add((CtType.Text, textPart, true, false, false, 20, true));
                                }
                                break;
                            case "math":
                                break;
                            case "cite":
                            case "p":
                                Logger.Log("p children:");
                                Logger.Log("   ", divChild.ChildNodes.Count);
                                foreach (var cc in divChild.ChildNodes) {
                                    Logger.Log("    >", cc.Name, ">", cc.InnerText);
                                    var cStyleString = cc.GetAttributeValue("style", "");
                                    var cStyle = (StyleRule) CssParser.Parse(".x{ " + cStyleString + " }").StyleRules.First();
                                    
                                    foreach (var textPart in cc.InnerText.Split('￼')) {
                                        var fontSize = 11.0;
                                        if (!string.IsNullOrWhiteSpace(cStyle.Style.FontSize)) {
                                            var parseSuccess = double.TryParse(cStyle.Style.FontSize.Replace("pt", ""), out fontSize);
                                            if (!parseSuccess) {
                                                fontSize = 11.0;
                                            }
                                        }
                                        currentTextContent.Add((
                                            CtType.Text, 
                                            textPart, 
                                            cStyle.Style.FontWeight.ToLower() == "bold", 
                                            cStyle.Style.FontStyle.ToLower() == "italic", 
                                            cStyle.Style.TextDecoration.ToLower() == "underline",
                                            fontSize,
                                            false
                                        ));
                                    }
                                }
                                currentTextContent.Add((CtType.Text, "", false, false, false, 11, true));
                                break;
                            case "br":
                                currentTextContent.Add((CtType.Text, "", false, false, false, 11, true));
                                break;
                            case "ol":
                            case "ul":
                                void HandleAsParentNode(HtmlNode current, int indent=0) {
                                    var hasRealChildren = current.HasChildNodes && !(current.ChildNodes.Count == 1 && current.ChildNodes[0].Name == "#text");
                                    Logger.Log($"{current.Name} ({hasRealChildren}) -> {current.InnerText}");
                                    if (!hasRealChildren) {
                                        currentTextContent.Add((CtType.Text, string.Concat(Enumerable.Repeat("   ", indent)) + " - " + current.InnerText, false, false, false, 11, true));
                                        Logger.Log("currentTextContent += ", current.InnerText); 
                                        return;
                                    }
                                    foreach (var listChild in current.ChildNodes) {
                                        if (listChild.NodeType != HtmlNodeType.Element) continue;
                                        HandleAsParentNode(listChild, indent+1);
                                    }
                                }
                                HandleAsParentNode(divChild);

                                break;
                            case "table":
                                var tableRtf = "";
                                const int rowWidth = 2000;
                                Logger.Log("table");
                                foreach (var tableChild in divChild.ChildNodes) {
                                    if (tableChild.Name == "#text") continue;
                                    Logger.Log("   tr", tableChild.Name);
                                    var header = @"\trowd\trgaph10\trpaddl100\trpaddr100\trpaddfl300\trpaddfr300";
                                    var content = "";
                                    var itr = 0;
                                    foreach (var rowChild in tableChild.ChildNodes) {
                                        if (rowChild.Name == "#text") continue;
                                        Logger.Log("      " + rowChild.Name, ">", rowChild.InnerText);
                                        itr += 1;
                                        header += $@"\cellx{rowWidth*itr}";
                                        content += @"\pard\tx720\sb100\sa100\cf1\" + rowChild.InnerText + @"\cell";
                                    }

                                    tableRtf += header + "\n" + content + "\n" + @"\row" + "\n";
                                }
                                currentTextContent.Add((CtType.RawRtf, tableRtf, false, false, false, 11, true));
                                break;
                            default:
                                Logger.Log("ArgumentOutOfRange", divChild.Name, logLevel: LogLevel.Warning);
                                break;
                        }
                    }
                    
                    if (currentTextContent.Count > 0) {
                        // add text model
                        AddTextModel();
                        currentTextContent.Clear();
                    }

                    return components;
                case "img":
                    // img containers
                    var imgSource = containerChild.GetAttributeValue("data-fullres-src", null);
                    var imgResult = await new OnenoteResourceContentRequestBuilder(imgSource, _graphClient).Request().GetAsync();

                    var outputPath = new ImageComponent(null).GetImageSavingPath();

                    var imgBytes = imgResult.ReadAllBytes();
                    LocalFileHelper.WriteFile(outputPath, imgBytes);

                    imgResult.Close();

                    if (styleString == "") {
                        Logger.Log("(img is using parent style)");
                        styleString = containerChild.ParentNode.GetAttributeValue("style", "");
                        style = (StyleRule) CssParser.Parse(".x{ " + styleString + " }").StyleRules.First();
                    }

                    float.TryParse(style.Style.Left.Replace("px", "").Replace("pt", ""), out x);
                    float.TryParse(style.Style.Top.Replace("px", "").Replace("pt", ""), out y);
                    
                    float.TryParse(containerChild.GetAttributeValue("width", "0.0"), out width); 
                    float.TryParse(containerChild.GetAttributeValue("height", "0.0"), out var height);
                    
                    Logger.Log($" > DocumentComponent: {outputPath} | X: {x}, Y: {y}, W: {width}, H: {height} ({styleString})");
                    return new List<ComponentModel> {
                        new ComponentModel {
                            Type = "DocumentComponent",
                            Content = outputPath,
                            PosX = x,
                            PosY = y,
                            SizeX = width,
                            SizeY = height
                        }
                    };
                case "br":
                    return new List<ComponentModel>();
                default:
                    Logger.Log("ArgumentOutOfRange", containerChild.Name, logLevel: LogLevel.Warning);
                    return new List<ComponentModel>();
            }
        }

        public static async Task<ParseResult> ParsePageContent(string pageContent) {
            var doc = new HtmlDocument();
            var inkXml = new XmlDocument();
            
            // split response
            var contentSplit = pageContent.Split('\n');
            var parts = new Dictionary<string, string>();
            if (contentSplit[0].StartsWith("--")) {
                for (var i = 0; i < contentSplit.Length; i++) {
                    if (contentSplit[i].StartsWith("--")) {
                        var splitType = Regex.Replace(contentSplit[i + 1], @"\r\n?|\n", "").Replace("Content-Type: ", "").Replace("; charset=utf-8", "");
                        if (string.IsNullOrWhiteSpace(splitType)) continue;
                        Logger.Log("   ", Regex.Replace(contentSplit[i], @"\r\n?|\n", ""));
                        Logger.Log("   ", splitType);
                        parts.Add(splitType, "");
                    } 
                    if (i != 0 && contentSplit[i - 1].StartsWith("--")) continue;
                    if (contentSplit[i].StartsWith("--")) continue;
                    if (string.IsNullOrWhiteSpace(Regex.Replace(contentSplit[i], @"\r\n?|\n", ""))) continue;

                    parts[parts.Keys.Last()] += contentSplit[i];
                }

                Logger.Log($"ResponseParts: {parts.Keys.ToFString()}", logLevel: LogLevel.Verbose);
                
                // load
                doc.LoadHtml(parts["text/html"]);
                inkXml.LoadXml(parts["application/inkml+xml"]);
            } else {
                Logger.Log("Response is not MultiPart", logLevel: LogLevel.Warning);
                doc.LoadHtml(pageContent);
                inkXml = null;
            }
            
            // parse html
            var title = System.Net.WebUtility.HtmlDecode(doc.DocumentNode.SelectSingleNode("//title").InnerText);
            
            var components = new List<ComponentModel>();
            var bodyNode = doc.DocumentNode.SelectSingleNode("//body");
            
            foreach (var containerChild in bodyNode.ChildNodes) {
                if (containerChild.NodeType != HtmlNodeType.Element) continue;
                var result = await ParsePageChild(containerChild);
                components.AddRange(result);
            }
            
            // parse xml
            var strokes = new List<StrokeModel>();
            
            // ReSharper disable once InvertIf
            if (inkXml != null) {
                var pens = new Dictionary<string, (Color, double, double, double, PenTip, PenType, bool, bool, bool)>();
                void HandleNode(XmlNode node) {
                    foreach (var n in node.ChildNodes) {
                        if (n is XmlNode childNode) {
                            switch (childNode.LocalName) {
                                case "trace":
                                    var traceValues = childNode.ChildNodes[0].Value;
                                    var traceSplits = traceValues.Split(',');
                                    var points = new List<float[]>();
                                    foreach (var tracePoint in traceSplits) {
                                        var tracePointParts = tracePoint.Trim().Split(' ');
                                        int x, y;
                                        switch (tracePointParts.Length) {
                                            case 3:
                                                // x, y, f
                                                x = int.Parse(tracePointParts[0]);
                                                y = int.Parse(tracePointParts[1]);
                                                var f = int.Parse(tracePointParts[2]);
                                                points.Add(new[] {(float) x, y, f});
                                                break;
                                            case 2:
                                                // x, y
                                                x = int.Parse(tracePointParts[0]);
                                                y = int.Parse(tracePointParts[1]);
                                                points.Add(new[] {(float) x, y});
                                                break;
                                            default:
                                                Logger.Log("ArgumentOutOfRange", "HandleNode", logLevel: LogLevel.Warning);
                                                break;
                                        }
                                    }

                                    Logger.Log("Adding stroke with properties from pen id", childNode.Attributes?["brushRef"].Value.Substring(1));
                                    var (color, width, height, transparency, penTip, penType, ignorePressure, antiAliased, fitToCurve) = pens[childNode.Attributes?["brushRef"].Value.Substring(1) ?? throw new Exception("brushRef was empty")];
                                    strokes.Add(new StrokeModel(
                                        points, Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B), width, height, transparency, penTip, penType, ignorePressure, antiAliased, fitToCurve
                                    ));
                                    break;
                                case "brush":
                                    var penId = childNode.Attributes?["xml:id"]?.Value;
                                    Logger.Log("brush", penId);
                                    color = Color.Black;
                                    width = 10.0;
                                    height = 10.0;
                                    transparency = 0.0;
                                    penTip = PenTip.Circle;
                                    penType = PenType.Default;
                                    ignorePressure = false;
                                    antiAliased = true;
                                    fitToCurve = false;
                                    foreach (var property in childNode.ChildNodes) {
                                        if (!(property is XmlNode propertyNode)) continue;
                                        if (propertyNode.Name == "#text") continue;
                                        var propName = propertyNode.Attributes?["name"].Value;
                                        var propValue = propertyNode.Attributes?["value"].Value;
                                        Logger.Log("propName:", propName);
                                        switch (propName) {
                                            case "width":
                                                width = double.Parse(propValue) / Configuration.OneNoteFactors.PenSizeFactor;
                                                break;
                                            case "height":
                                                height = double.Parse(propValue) / Configuration.OneNoteFactors.PenSizeFactor;
                                                break;
                                            case "color":
                                                color = Color.FromHex(propValue);
                                                break;
                                            case "transparency":
                                                transparency = double.Parse(propValue);
                                                break;
                                            case "tip":
                                                switch (propValue) {
                                                    case "ellipse":
                                                        penTip = PenTip.Circle;
                                                        break;
                                                    case "rectangle":
                                                        penTip = PenTip.Rectangle;
                                                        break;
                                                    default:
                                                        Logger.Log("Error Property: ", propName, logLevel: LogLevel.Error);
                                                        Logger.Log("ArgumentOutOfRange", propValue, logLevel: LogLevel.Warning);
                                                        break;
                                                }
                                                break;
                                            case "rasterOp":
                                                switch (propValue) {
                                                    case "copyPen":
                                                        penType = PenType.Default;
                                                        break;
                                                    case "maskPen":
                                                        penType = PenType.Marker;
                                                        break;
                                                    default:
                                                        Logger.Log("Error Property: ", propName, logLevel: LogLevel.Error);
                                                        Logger.Log("ArgumentOutOfRange", propValue, logLevel: LogLevel.Warning);
                                                        break;
                                                }
                                                break;
                                            case "ignorePressure":
                                                ignorePressure = bool.Parse(propValue);
                                                break;
                                            case "antiAliased":
                                                antiAliased = bool.Parse(propValue);
                                                break;
                                            case "fitToCurve":
                                                fitToCurve = bool.Parse(propValue);
                                                break;
                                            default:
                                                Logger.Log("ArgumentOutOfRange", propValue, logLevel: LogLevel.Warning);
                                                break;
                                        }
                                    }
                                    pens.Add(penId, (color, width, height, transparency, penTip, penType, ignorePressure, antiAliased, fitToCurve));
                                    break;
                            }

                            if (childNode.HasChildNodes) {
                                HandleNode(childNode);
                            }
                        } else {
                            Logger.Log("ArgumentOutOfRange", n.ToString(), logLevel: LogLevel.Warning);
                        }
                    }
                }

                HandleNode(inkXml);
                Logger.Log("parsed strokes:", strokes.Count, logLevel: LogLevel.Verbose);
            }

            return new ParseResult(title, components, strokes);
        }
    }
}