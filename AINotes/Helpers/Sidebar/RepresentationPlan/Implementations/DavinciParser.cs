using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AINotes.Helpers.Sidebar.RepresentationPlan.Models;
using Helpers;
using Helpers.Extensions;
using HtmlAgilityPack;

namespace AINotes.Helpers.Sidebar.RepresentationPlan.Implementations {
    public class DavinciParser : IRepresentationPlanParser {
        private readonly HtmlWeb _htmlWeb = new HtmlWeb();
        
        public async Task<Dictionary<string, List<RepresentationItemModel>>> GetRepresentations(string url, string grade) {
            var result = new Dictionary<string, List<RepresentationItemModel>>();
            try {
                // fetch overview
                var overviewDocument = await _htmlWeb.LoadFromWebAsync(url);
                
                var dayNodes = overviewDocument.DocumentNode.SelectNodes("//*[contains(concat(\" \", normalize-space(@class), \" \"), \" day \")]");
                var dayLinks = dayNodes.Select(node => node.Attributes.FirstOrDefault(attr => attr.Name == "onclick")?.Value.Replace("window.location.href=", "").Replace("'", ""));

                foreach (var dayLink in dayLinks) {
                    if (string.IsNullOrWhiteSpace(dayLink)) continue;
                    Logger.Log("[DavinciParser]", "Getting Substitutions for", dayLink);
                    var dayDocument = await _htmlWeb.LoadFromWebAsync(url + "/" + dayLink);

                    var dateString = dayDocument.DocumentNode.SelectSingleNode("/html/body/div[3]/div/div[1]/div/h1").InnerText.Replace(new [] { "\n", "\r" }, "");
                    
                    // var tableHeader = dayDocument.DocumentNode.SelectSingleNode("//thead/tr").SelectNodes("th").Select(itm => itm.InnerText.Replace(new [] { "\n", "\r", " " }, "")).ToList();
                    var tableRows = dayDocument.DocumentNode.SelectNodes("//tbody/tr");
                    
                    var dayModels = new List<RepresentationItemModel>();
                    // ReSharper disable once LoopCanBeConvertedToQuery
                    foreach (var row in tableRows) {
                        var rowItems = row.SelectNodes("td").Select(itm => itm.InnerText.Replace(new [] { "\n", "\r" }, "")).ToList();
                        
                        // var rowDict = new Dictionary<string, string>();
                        // for (var i = 0; i < tableHeader.Count; i++) {
                        //     rowDict.Add(tableHeader[i], rowItems[i]);
                        // }
                        
                        
                        var rowModel = new RepresentationItemModel {
                            Class = rowItems[0],
                            Day = rowItems[1],
                            Position = rowItems[2],
                            Subject = rowItems[3],
                            Room = rowItems[4],
                            VSubject = rowItems[5],
                            VRoom = rowItems[6],
                            Kind = rowItems[7],
                            Info = rowItems[8],
                            Comment = rowItems[9],
                            Message = rowItems[10],
                        };
                        
                        // filter by grade
                        if (!string.IsNullOrWhiteSpace(grade)) {
                            if (!rowModel.Class.Contains(grade)) continue;
                        }
                        
                        dayModels.Add(rowModel);
                    }

                    if (dayModels.Count != 0) {
                        result.Add(dateString, dayModels);
                    }
                }
            } catch (Exception ex) {
                Logger.Log("[DavinciParser]", "Exception:", ex.ToString(), logLevel: LogLevel.Error);
            }
            
            return result;
        }
    }
}