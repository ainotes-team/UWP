using Helpers.Lists;
using MaterialComponents;
using Newtonsoft.Json;
using SQLite;

namespace AINotes.Models {
    public class DirectoryModel : IFMSListableModel, IMDDataModel {
        [PrimaryKey, AutoIncrement, Unique]
        [JsonProperty("directory_id")]
        public int DirectoryId { get; set; }

        [Indexed]
        [JsonProperty("parent_directory_id")]
        public int ParentDirectoryId { get; set; }
        
        [Indexed]
        [JsonProperty("directory_name")]
        public string Name { get; set; }
        
        [JsonProperty("file_subject")]
        public string Subject { get; set; }
        
        [JsonProperty("hex_color")]
        public string HexColor { get; set; }

        [JsonIgnore]
        public string RemoteId { get; set; }

        // fms compatibility
        [Ignore]
        [JsonIgnore]
        public long LastChangedDate { get; set; } = -1;

        [Ignore]
        [JsonIgnore]
        public long CreationDate { get; set; } = -1;
        
        [Ignore]
        [JsonIgnore]
        public ObservableList<int> Labels { get; } = new ObservableList<int>();

        [Ignore]
        [JsonIgnore]
        public string Owner { get; set; } = "me";

        [Ignore]
        [JsonIgnore]
        public string Status { get; set; } = "saved locally";
        
        [Ignore]
        [JsonIgnore]
        public bool IsFavorite { get; set; } = false;

    }
}