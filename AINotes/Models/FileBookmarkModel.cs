using System;
using Newtonsoft.Json;

namespace AINotes.Models {
    public class FileBookmarkModel {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("x")]
        public double ScrollX { get; set; }
        
        [JsonProperty("y")]
        public double ScrollY { get; set; }
        
        [JsonProperty("z")]
        public float Zoom { get; set; }

        public override string ToString() => $"{Name} ({Math.Round(ScrollX)}|{Math.Round(ScrollY)})";
    }
}