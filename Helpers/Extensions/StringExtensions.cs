using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Helpers.Extensions {
    public static class StringExtensions {
        public static string Serialize(this string self) {
            return JsonConvert.SerializeObject(self);
        }

        public static T Deserialize<T>(this string self) {
            return JsonConvert.DeserializeObject<T>(self);
        }
        
        public static bool EndsWithAny(this string self, IEnumerable<string> endings) {
            return endings.Any(self.EndsWith);
        }
        
        public static string Replace(this string self, IEnumerable<string> replace, string replacement) {
            return replace.Aggregate(self, (current, r) => current.Replace(r, replacement));
        }
        
        public static string Replace(this string self, Dictionary<string, string> replacementDict) {
            foreach (var (oldString, newString) in replacementDict) {
                self = self.Replace(oldString, newString);
            }
            return self;
        }

        public static string FirstCharToUpper(this string input) {
            switch (input) {
                case null:
                    throw new ArgumentNullException(nameof(input));
                case "":
                    throw new ArgumentException(@$"{nameof(input)} cannot be empty", nameof(input));
                default:
                    return input.First().ToString().ToUpper() + input.Substring(1);
            }
        }
        
        public static string Truncate(this string value, int maxLength, string ifTruncated="") {
            if (string.IsNullOrEmpty(value)) return value;
            var ret = value.Length <= maxLength ? value : value.Substring(0, maxLength);
            if (ret != value) {
                ret += ifTruncated;
            }
            return ret;
        }

        public static string Join(this string value, IList list) {
            var result = "";
            for (var i = 0; i < list.Count-1; i++) {
                result += list[i] + value;
            }

            result += list[list.Count - 1];

            return result;
        }
    }
}