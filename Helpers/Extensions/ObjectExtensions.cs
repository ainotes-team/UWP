using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Helpers.Extensions {
    public static class ObjectExtensions {
        public static string Serialize<T>(this T self, JsonSerializerSettings settings = null) {
            if (self is Task) {
                Logger.Log("Can not serialize Task - Use await!", logLevel: LogLevel.Error);
            }

            return settings == null ? JsonConvert.SerializeObject(self) : JsonConvert.SerializeObject(self, settings);
        }
    }
}