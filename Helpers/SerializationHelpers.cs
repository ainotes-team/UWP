using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Helpers {
    public class IgnorePropertiesResolver : DefaultContractResolver {
        private readonly IEnumerable<string> _propsToIgnore;

        public IgnorePropertiesResolver(IEnumerable<string> propNamesToIgnore) {
            _propsToIgnore = propNamesToIgnore;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
            var property = base.CreateProperty(member, memberSerialization);
            property.ShouldSerialize = obj => !_propsToIgnore.Contains(property.PropertyName);
            return property;
        }
    }
}