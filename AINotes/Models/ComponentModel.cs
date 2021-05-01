using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AINotes.Components;
using AINotes.Helpers;
using AINotesCloud.Models;
using Helpers;
using Helpers.Extensions;
using Newtonsoft.Json;
using SQLite;

namespace AINotes.Models {
    [Table("PluginModel")]
    public class ComponentModel {
        public ComponentModel() {
            var currentTimeMillis = Time.CurrentTimeMillis();

            ContentLastChanged = currentTimeMillis;
            PositionLastChanged = currentTimeMillis;
            SizeLastChanged = currentTimeMillis;
            ZIndexLastChanged = currentTimeMillis;
            DeletionLastChanged = currentTimeMillis;
        }

        #region Events

        public event Action<int> ComponentIdChanged;

        #endregion
        
        #region Ids

        [Column("PluginId"), PrimaryKey, AutoIncrement, Unique]
        [JsonProperty("plugin_id")]
        public int ComponentId { get; set; }
        
        [Indexed]
        [JsonProperty("file_id")]
        public int FileId { get; set; }

        [JsonIgnore]
        [Unique]
        public string RemoteId { get; set; }

        [Ignore]
        [JsonProperty("live_id")]
        public int LiveId { get; set; }

        #endregion

        #region DisplayProperties

        private double _posX = double.NaN;
        [JsonProperty("pos_x")]
        public double PosX {
            get => _posX;
            set {
                if (_posX == double.NaN) {
                    _posX = value;
                } else {
                    if (value != _posX) {
                        _posX = value;
                        PositionLastChanged = Time.CurrentTimeMillis();
                    }
                }
            }
        }

        private double _posY = double.NaN;

        [JsonProperty("pos_y")]
        public double PosY {
            get => _posY;
            set {
                if (_posY == double.NaN) {
                    _posY = value;
                } else {
                    if (value != _posY) {
                        _posY = value;
                        PositionLastChanged = Time.CurrentTimeMillis();
                    }
                }
            }
        }
        
        
        private double _sizeX = double.NaN;

        [JsonProperty("size_x")]
        public double SizeX {
            get => _sizeX;
            set {
                if (_sizeX == double.NaN) {
                    _sizeX = value;
                } else {
                    if (value != _sizeX) {
                        _sizeX = value;
                        SizeLastChanged = Time.CurrentTimeMillis();
                    }
                }
            }
        }
        
        
        private double _sizeY = double.NaN;

        [JsonProperty("size_y")]
        public double SizeY {
            get => _sizeY;
            set {
                if (_sizeY == double.NaN) {
                    _sizeY = value;
                } else {
                    if (value != _sizeY) {
                        _sizeY = value;
                        SizeLastChanged = Time.CurrentTimeMillis();
                    }
                }
            }
        }

        
        private int _zIndex = int.MinValue;

        [JsonProperty("z_index")]
        public int ZIndex {
            get => _zIndex;
            set {
                if (_zIndex == int.MinValue) {
                    _zIndex = value;
                } else {
                    if (value != _zIndex) {
                        _zIndex = value;
                        ZIndexLastChanged = Time.CurrentTimeMillis();
                    }
                }
            }
        }
        
        private string _content;

        [JsonProperty("content")]
        public string Content {
            get => _content;
            set {
                if (_content == null) {
                    _content = value;
                } else {
                    if (value != _content) {
                        _content = value;
                        ContentLastChanged = Time.CurrentTimeMillis();
                    }
                }
            }
        }
        
        [JsonIgnore]
        private string _type;
        
        [JsonProperty("plugin_type")]
        public string Type {
            get => _type;
            // backwards compatibility
            set => _type = value.Replace(new Dictionary<string, string> {
                {"TextPlugin", "TextComponent"},
                {"DocumentPlugin", "ImageComponent"},
            });
        }

        [JsonIgnore]
        private bool _deleted;
        
        [JsonProperty("deleted")]
        public bool Deleted {
            get => _deleted;
            set {
                if (value != _deleted) {
                    _deleted = value;
                    DeletionLastChanged = Time.CurrentTimeMillis();
                }
            }
        }

        [Ignore]
        [JsonIgnore]
        public (double, double) Position {
            set => (PosX, PosY) = value;
            get => (PosX, PosY);
        }
        
        [Ignore]
        [JsonIgnore]
        public (double, double) Size {
            set => (SizeX, SizeY) = value;
            get => (SizeX, SizeY);
        }

        [Ignore]
        [JsonIgnore]
        public double[] Rectangle => new[] {PosX, PosY, SizeX, SizeY};

        [Ignore]
        [JsonIgnore]
        public RectangleD Bounds => new RectangleD(PosX, PosY, SizeX, SizeY);

        #endregion

        #region Timestamps

        [JsonProperty("lastSynced")]
        public long LastSynced { get; set; }
        
        [JsonProperty("positionLastChanged")]
        public long PositionLastChanged { get; set; }
        
        [JsonProperty("sizeLastChanged")]
        public long SizeLastChanged { get; set; }
        
        [JsonProperty("contentLastChanged")]
        public long ContentLastChanged { get; set; }
        
        [JsonProperty("zIndexLastChanged")]
        public long ZIndexLastChanged { get; set; }
        
        [JsonProperty("deletionLastUpdated")] 
        public long DeletionLastChanged { get; set; }

        [Ignore]
        [JsonIgnore]
        public long LastChanged => new[] {PositionLastChanged, SizeLastChanged, ContentLastChanged, ZIndexLastChanged}.Max();

        #endregion

        #region Methods

        public Component ToComponent() => (Component) Activator.CreateInstance(ComponentManager.ComponentTypesByName[Type], this);

        private async Task<string> GetRemoteFileId() {
            var fileModel = await FileHelper.GetFileAsync(FileId);
            return fileModel.RemoteId;
        }

        public override string ToString() => JsonConvert.SerializeObject(this);
        public string ToJson() => JsonConvert.SerializeObject(this);

        public async Task SaveAsync() {
            if (ComponentId == -1) { 
                ComponentId = await FileHelper.CreateComponentAsync(this);
                ComponentIdChanged?.Invoke(ComponentId);
            } else {
                await FileHelper.UpdateComponentAsync(this);
            }
        }

        public async Task<RemoteComponentModel> ToRemoteComponentModelAsync(bool ignoreRemoteId = false) {
            var remoteComponentModel = new RemoteComponentModel {
                Content = Content,
                Rectangle = new []{ PosX, PosY, SizeX, SizeY },
                Type = Type,
                ZIndex = ZIndex,
                RemoteFileId = await GetRemoteFileId(),
                Deleted = Deleted
            };

            if (!ignoreRemoteId) remoteComponentModel.Id = RemoteId;
            return remoteComponentModel;
        }

        public static async Task<ComponentModel> FromRemoteComponentModelAsync(RemoteComponentModel remoteComponentModel) {
            var currentTimeMillis = Time.CurrentTimeMillis();
            var localFileId = await FileModel.GetLocalFileId(remoteComponentModel.RemoteFileId);
            return new ComponentModel {
                Content = remoteComponentModel.Content,
                PosX = remoteComponentModel.Rectangle[0],
                PosY = remoteComponentModel.Rectangle[1],
                SizeX = remoteComponentModel.Rectangle[2],
                SizeY = remoteComponentModel.Rectangle[3],
                ZIndex = remoteComponentModel.ZIndex,
                Type = remoteComponentModel.Type,
                Deleted = remoteComponentModel.Deleted,
                RemoteId = remoteComponentModel.Id,
                FileId = localFileId,
                LastSynced = currentTimeMillis,
                ContentLastChanged = currentTimeMillis,
                PositionLastChanged = currentTimeMillis,
                SizeLastChanged = currentTimeMillis,
                ZIndexLastChanged = currentTimeMillis,
            };
        }

        #endregion
    }
}