using Newtonsoft.Json;

namespace AINotesCloud.Models {
    public class RemoteFilePermission {
        [JsonProperty("_id")]
        public string PermissionId { get; set; }
        
        // user <-> file
        [JsonProperty("user")]
        public string UserId { get; set; }
        
        [JsonProperty("file")]
        public string FileId { get; set; }
        
        // user permission level
        [JsonProperty("userPermission")]
        public string UserPermission { get; set; }
        
        [JsonProperty("accepted")]
        public bool Accepted { get; set; }
    }
}