using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace FullTrustComponent {
    public static class InkImage {
        public static BitmapFrame MergeInk(StrokeCollection ink) {
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen()) {
                // drawingContext.DrawRectangle(new SolidColorBrush(Colors.Blue), new Pen(new SolidColorBrush(Colors.Brown), 2.0), new Rect(0, 0, (int) (ink.GetBounds().X + ink.GetBounds().Width), (int) (ink.GetBounds().Y + ink.GetBounds().Height)));

                const double f = 96.0 / 71.0;
                
                var scaleMatrix = Matrix.Identity;
                scaleMatrix.Scale(f, f);
                ink.Transform(scaleMatrix, true);
                
                ink.Draw(drawingContext);

                drawingContext.Close();
                
                var bitmap = new RenderTargetBitmap((int) (ink.GetBounds().X + ink.GetBounds().Width), (int) (ink.GetBounds().Y + ink.GetBounds().Height), 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(drawingVisual);
                return BitmapFrame.Create(bitmap);
            }
        }
    }
    
    public static class FileFormatHelper {
        public static bool XpsToPdf(string inPath, string outPath) {
            try {
                PdfSharp.Xps.XpsConverter.Convert(inPath, outPath, 0);
            } catch (Exception ex) {
                Console.WriteLine(@"FileFormatHelper: XpsToPdf - Exception: {0}", ex);
                return false;
            }

            return true;
        }
        
        public static bool JsonToPdf(string noteJson, string backgroundImagePath, string backgroundImagePosString, string outPath) {
            try {
                Console.WriteLine(@"FileFormatHelper: JsonToPdf to {0}", outPath);

                // load json
                var fileDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(noteJson);
                foreach (var kv in fileDict) {
                    Console.WriteLine(@"{0} => {1}", kv.Key, kv.Value);
                }

                var title = (string) fileDict["file_name"];
                // var lineMode = (long) fileDict["line_mode"];
                var strokeJson = (string) fileDict["stroke_content"];
                var componentJson = (string) fileDict["component_models"];

                var strokes = JsonConvert.DeserializeObject<byte[]>(strokeJson);
                var components = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(componentJson);
                
                StrokeCollection loadedStrokes;
                using (var strokeStream = new MemoryStream(strokes)) {
                    loadedStrokes = new StrokeCollection(strokeStream);
                }

                var strokeImg = InkImage.MergeInk(loadedStrokes);
                
                var (bgPosX, bgPosY) = JsonConvert.DeserializeObject<(double, double)>(backgroundImagePosString);
                var og = new BitmapImage(new Uri(backgroundImagePath));
                Console.WriteLine(@"OG {0}|{1}", og.DpiX, og.DpiY);

                // create document
                var document = new PdfDocument {
                    PageLayout = PdfPageLayout.SinglePage,
                    Settings = {
                        TrimMargins = new TrimMargins {
                            All = 0,
                        }
                    },
                    Info = {
                        Title = title,
                        Creator = "AINotes"
                    },
                };

                var page = document.AddPage();
                var gfx = XGraphics.FromPdfPage(page);

                // set page size
                var maxXWithWidth = 0.0;
                var maxYWithHeight = 0.0;
                foreach (var component in components) {
                    var x = (double) component["pos_x"];
                    var y = (double) component["pos_y"];
                    var w = (double) component["size_x"];
                    var h = (double) component["size_y"];

                    if (x + w > maxXWithWidth) maxXWithWidth = x + w;
                    if (y + h > maxYWithHeight) maxYWithHeight = y + h;
                }

                maxXWithWidth = Math.Max(maxXWithWidth, bgPosX + strokeImg.PixelWidth);
                maxYWithHeight = Math.Max(maxYWithHeight, bgPosY + strokeImg.PixelHeight);

                maxXWithWidth = Math.Max(maxXWithWidth, loadedStrokes.GetBounds().X);
                maxYWithHeight = Math.Max(maxYWithHeight, loadedStrokes.GetBounds().Y);

                page.Width = Math.Max(500, maxXWithWidth);
                page.Height = Math.Max(500, maxYWithHeight);
                page.TrimMargins = new TrimMargins {
                    All = 0
                };

                // add components
                var font = new XFont("Segoe UI", 10.5, XFontStyle.Regular);
                foreach (var component in components) {
                    var type = (string) component["plugin_type"];
                    var content = (string) component["content"];
                    var x = (double) component["pos_x"];
                    var y = (double) component["pos_y"];
                    var w = (double) component["size_x"];
                    var h = (double) component["size_y"];
                    // gfx.DrawRectangle(new XSolidBrush(XColors.Purple), new XRect(x, y, w, h));

                    switch (type) {
                        case "TextComponent":
                            Console.WriteLine(@"FileFormatHelper: JsonToPdf - TextComponent @ ({0}|{1}) | {2}, {3}", x, y, w, h);
                            const string basicRtfRegex = @"\{\*?\\[^{}]+}|[{}]|\\\n?[A-Za-z]+\n?(?:-?\d+)?[ ]?";
                            var result = new Regex(basicRtfRegex).Replace(content, "");
                            gfx.DrawString(result, font, XBrushes.Black, new XRect(x, y, w, h), new XStringFormat {
                                LineAlignment = XLineAlignment.Near,
                                Alignment = XStringAlignment.Near,
                                FormatFlags = XStringFormatFlags.MeasureTrailingSpaces,
                            });
                            break;
                        case "ImageComponent":
                            Console.WriteLine(@"FileFormatHelper: JsonToPdf - ImageComponent @ ({0}|{1}) | {2}, {3}", x, y, w, h);
                            var imageBytes = JsonConvert.DeserializeObject<byte[]>(content);
                            using (var byteStream = new MemoryStream(imageBytes)) {
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.StreamSource = byteStream;
                                // bitmap.DecodePixelHeight = (int) h;
                                // bitmap.DecodePixelWidth = (int) w;
                                bitmap.EndInit();
                                using (var img = XImage.FromBitmapSource(bitmap)) {
                                    // gfx.DrawImage(img, new XPoint(x, y));
                                    gfx.DrawImage(img, new XRect(x, y, w, h));
                                }
                            }

                            break;
                        default:
                            Console.WriteLine(@"FileFormatHelper: JsonToPdf - TODO: {0}", type);
                            break;
                    }
                }

                using (var img = XImage.FromBitmapSource(strokeImg)) {
                    gfx.DrawImage(img, new XPoint(0, 0));
                }

                // debug coordinate system
                // var f = new XFont("Segoe UI", 8.5, XFontStyle.Regular);
                // for (var iX = 0; iX < page.Width; iX += 100) {
                //     for (var iY = 0; iY < page.Height; iY += 50) {
                //         gfx.DrawString($"{iX}|{iY}", f, XBrushes.Black, new XPoint(iX, iY));
                //     }
                // }

                // save
                document.Save(outPath);

                // debug open
                try {
                    Process.Start(outPath);
                } catch (Exception openEx) {
                    Console.WriteLine(@"FileFormatHelper: JsonToPdf - Exception at Process.Start: {0}", openEx);
                }
                
                document.Dispose();
                gfx.Dispose();
            } catch (Exception ex) {
                Console.WriteLine(@"FileFormatHelper: JsonToPdf - Exception: {0}", ex);
                return false;
            }
            
            return true;
        }
    }
}