using Newtonsoft.Json;

namespace AINotesCloud.Models {
    public class RemoteComponentModel {
        #region Ids

        [JsonProperty("_id")]
        public string Id { get; set; }
        
        [JsonProperty("fileId")]
        public string RemoteFileId { get; set; }

        #endregion

        #region DisplayProperties

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("rectangle")]
        public double[] Rectangle { get; set; }
        
        [JsonProperty("zIndex")]
        public int ZIndex { get; set; }
        
        [JsonProperty("deleted")]
        public bool Deleted { get; set; }

        #endregion
        
        #region Timestamps

        [JsonProperty("lastUpdated")]
        public long LastUpdated { get; set; }
        
        [JsonProperty("positionLastUpdated")]
        public long PositionLastUpdated { get; set; }
        
        [JsonProperty("sizeLastUpdated")]
        public long SizeLastUpdated { get; set; }
        
        [JsonProperty("contentLastUpdated")]
        public long ContentLastUpdated { get; set; }
        
        [JsonProperty("zIndexLastUpdated")]
        public long ZIndexLastUpdated { get; set; }
        
        [JsonProperty("deletionLastUpdated")] 
        public long DeletionLastUpdated { get; set; }

        #endregion
    }
}