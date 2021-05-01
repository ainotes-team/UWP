using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using AINotes.Helpers;
using Helpers.Extensions;
using Helpers.Lists;
using AINotes.Models.Enums;
using AINotesCloud;
using AINotesCloud.Models;
using MaterialComponents;
using Newtonsoft.Json;
using SQLite;

namespace AINotes.Models {
    public class FileModel : IFMSListableModel, IMDDataModel {
        [PrimaryKey, AutoIncrement, Unique]
        [JsonProperty("file_id")]
        public int FileId { get; set; }

        [Indexed]
        [JsonProperty("parent_directory_id")]
        public int ParentDirectoryId { get; set; }
        
        [Indexed]
        [JsonProperty("file_name")]
        public string Name { get; set; }

        [JsonIgnore]
        public string Subject {
            get => null;
            set {
                if (value == null || value == "none") return;
                var idx = new Dictionary<string, Color> {
                    {"none", ColorCreator.FromHex("#FFFFFF")},
                    {"maths", ColorCreator.FromHex("#FCB450")},
                    {"german", ColorCreator.FromHex("#5851DB")},
                    {"english", ColorCreator.FromHex("#5CBC63")},
                    {"latin", ColorCreator.FromHex("#FC5650")},
                    {"french", ColorCreator.FromHex("#E56588")},
                    {"physics", ColorCreator.FromHex("#5D9EBE")},
                    {"biology", ColorCreator.FromHex("#96C03A")},
                    {"chemistry", ColorCreator.FromHex("#963AC0")},
                    {"informatics", ColorCreator.FromHex("#898F8F")},
                    {"history", ColorCreator.FromHex("#AE8B65")},
                    {"socialstudies", ColorCreator.FromHex("#B0DDF0")},
                    {"geography", ColorCreator.FromHex("#FAC9A1")},
                    {"religion", ColorCreator.FromHex("#4E5357")},
                    {"philosophy", ColorCreator.FromHex("#7D82BA")},
                    {"music", ColorCreator.FromHex("#FEF2B6")},
                    {"art", ColorCreator.FromHex("#FAE355")},
                }.Keys.ToList().IndexOf(value) + 1;
                if (idx > 0) Labels.Add(idx);
            }
        }

        [JsonIgnore]
        public bool IsFavorite { get; set; }

        [JsonIgnore]
        public bool IsShared { get; set; }

        private bool _labelsSet;
        [JsonIgnore]
        public string LabelsString {
            get => Labels.Serialize();
            set {
                if (!_labelsSet) {
                    _labelsSet = true;
                    Labels.AddRange(value?.Deserialize<List<int>>() ?? new List<int>());
                    return;
                }
                var newItems = value?.Deserialize<List<int>>() ?? new List<int>();
                foreach (var oldItem in Labels.ToList()) {
                    if (newItems.Contains(oldItem)) {
                        newItems.Remove(oldItem);
                    } else {
                        Labels.Remove(oldItem);
                    }
                }
                Labels.AddRange(newItems);
            }
        }

        [JsonIgnore]
        public string SerializedBookmarks { get; set; } = "[]";

        [Ignore]
        [JsonIgnore]
        public ObservableList<int> Labels { get; } = new ObservableList<int> { PreventDuplicates = true };

        [JsonProperty("creation_date")]
        public long CreationDate { get; set; }
        
        [JsonProperty("last_changed_date")]
        public long LastChangedDate { get; set; }

        [JsonProperty("line_mode")]
        public DocumentLineMode LineMode { get; set; } = DocumentLineMode.GridMedium;

        [JsonProperty("stroke_content")]
        public string StrokeContent { get; set; }
        
        [JsonProperty("deleted")]
        public bool Deleted { get; set; }
        
        [JsonProperty("remote_account_id")]
        public string RemoteAccountId { get; set; }

        [JsonIgnore]
        public float Zoom { get; set; } = 1;

        [JsonIgnore]
        public double ScrollX { get; set; } = 0;
        
        [JsonIgnore]
        public double ScrollY { get; set; } = 0;

        [JsonIgnore]
        public double? Width { get; set; } = null;
        
        [JsonIgnore]
        public double? Height { get; set; } = null;

        [Ignore]
        [JsonIgnore]
        public string InternalComponentModels { get; set; }

        [Ignore]
        [JsonProperty("component_models")]
        public string ComponentModels {
            get => InternalComponentModels;
            set => InternalComponentModels = value;
        }

        [JsonIgnore]
        [Unique]
        public string RemoteId { get; set; }
        
        [JsonIgnore]
        public long LastSynced { get; set; }

        [JsonIgnore]
        public string Owner { get; set; }

        [Ignore]
        [JsonIgnore]
        public string Status {
            get => RemoteId != null
                ? LastSynced == LastChangedDate 
                    ? "Synced" 
                    : SynchronizationService.IsRunning 
                        ? "Syncing..."
                        : "Waiting for connection..." 
                : "Saved locally";
            set {}
        }

        [Ignore]
        [JsonIgnore]
        public int Id => FileId;

        public async Task<List<ComponentModel>> GetComponentModels() => await FileHelper.GetFileComponentsAsync(FileId);

        public override string ToString() => this.Serialize();
        
        public static async Task<int> GetLocalFileId(string remoteId) {
            return await FileHelper.GetLocalFileFromRemoteId(remoteId);
        }

        public RemoteFileModel ToRemoteFileModel() {
            return new RemoteFileModel {
                RemoteId = RemoteId,
                Name = Name,
                Subject = Subject,
                CreationDate = CreationDate,
                LineMode = (int) LineMode,
                LastChangedDate = LastChangedDate,
                StrokeContent = StrokeContent,
                Deleted = Deleted
            };
        }

        public static FileModel FromRemoteFileModel(RemoteFileModel remoteFileModel) {
            return new FileModel {
                RemoteId = remoteFileModel.RemoteId,
                Name = remoteFileModel.Name,
                Subject = remoteFileModel.Subject ?? "none",
                CreationDate = remoteFileModel.CreationDate,
                LastChangedDate = remoteFileModel.LastChangedDate,
                LineMode = (DocumentLineMode) remoteFileModel.LineMode,
                StrokeContent = remoteFileModel.StrokeContent,
                FileId = -1,
                Deleted = remoteFileModel.Deleted,
                ParentDirectoryId = 0,
            };
        }
    }
}