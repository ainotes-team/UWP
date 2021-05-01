using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Helpers;
using Helpers.Extensions;

namespace AINotesCloud.Models {
    public class RemoteFileModel {
        [JsonProperty("_id")] public string RemoteId { get; set; }

        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("subject")] public string Subject { get; set; }

        [JsonProperty("creationDate")] public long CreationDate { get; set; }

        [JsonProperty("lastChangedDate")] public long LastChangedDate { get; set; }

        [JsonProperty("lineMode")] public int LineMode { get; set; }

        [JsonProperty("strokeContent")] public string StrokeContent { get; set; }

        [JsonProperty("deleted")] public bool Deleted { get; set; }

        public string ToJson(bool includeRemoteId) {
            string result;
            if (!includeRemoteId) {
                result = JsonConvert.SerializeObject(this, new JsonSerializerSettings {
                    ContractResolver = new IgnorePropertiesResolver(new[] {"_id", "RemoteId"})
                });
            } else {
                result = this.Serialize();
            }

            return result;
        }

        public async Task<List<RemoteComponentModel>> GetRemoteComponentModels() {
            return await SynchronizationService.CloudApi.GetRemoteComponents(RemoteId);
        }

        public static async Task<List<RemoteComponentModel>> GetRemoteComponentModels(string remoteId) {
            return await SynchronizationService.CloudApi.GetRemoteComponents(remoteId);
        }

        public async Task<List<RemoteFilePermission>> GetRemotePermissionModels() {
            return await SynchronizationService.CloudApi.GetRemotePermissions(RemoteId);
        }

        public static async Task<List<RemoteFilePermission>> GetRemotePermissionModels(string remoteId) {
            return await SynchronizationService.CloudApi.GetRemotePermissions(remoteId);
        }
    }
}