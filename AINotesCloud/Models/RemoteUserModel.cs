using Newtonsoft.Json;

namespace AINotesCloud.Models {
    public class RemoteUserModel {
        [JsonProperty("_id")]
        public string RemoteId { get; set; }
        
        [JsonProperty("email")]
        public string Email { get; set; }
        
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
        
        [JsonProperty("profilePicture")]
        public string ProfilePicture { get; set; }
    }
}