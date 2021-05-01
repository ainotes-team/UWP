using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Helpers.Extensions {
    public static class EnumerableExtensions {
        public static string ToFString<TKey, TValue>(this Dictionary<TKey, TValue> dict, string separator=", ") {
            if (dict == null) return "";
            var returnValue = "{";
            foreach (var (k, v) in dict) {
                returnValue += k + ": " + v + separator;
            }
            if (returnValue.EndsWith(separator)) {
                returnValue = returnValue.Remove(returnValue.Length - separator.Length);
            }
            returnValue += "}";
            return returnValue;
        }
        
        public static string ToFString(this IEnumerable arr, string separator=", ") {
            if (arr == null) return "";
            var returnValue = "[";
            returnValue = arr.Cast<object>().Aggregate(returnValue, (current, element) => current + (element + separator));
            if (returnValue.EndsWith(separator)) {
                returnValue = returnValue.Remove(returnValue.Length - 2);
            }
            returnValue += "]";
            return returnValue;
        }

        public static string ToFString(this Array arr) {
            if (arr == null) return "";
            var returnValue = "[";
            returnValue = arr.Cast<object>().Aggregate(returnValue, (current, element) => current + (element + ", "));
            if (returnValue.EndsWith(", ")) {
                returnValue = returnValue.Remove(returnValue.Length - 2);
            }
            returnValue += "]";
            return returnValue;
        }
        
        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value) {
            key = tuple.Key;
            value = tuple.Value;
        }

        public static TKey ReverseLookup<TKey, TValue>(this Dictionary<TKey, TValue> dict, TValue value)  {
            return dict.FirstOrDefault(kv => Equals(kv.Value, value)).Key;
        }

        public static bool ContainsAny<T>(this List<T> enumerable, IEnumerable<T> values) => values.Any(enumerable.Contains);
        
        public static bool ContainsAll<T>(this List<T> enumerable, IEnumerable<T> values) => values.All(enumerable.Contains);
         
        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> values) {
            foreach (var value in values) {
                collection.Add(value);
            }
        }
        
        public static void AddRange<T>(this UIElementCollection collection, IEnumerable<T> values) where T : UIElement {
            foreach (var value in values) {
                collection.Add(value);
            }
        }
        
        public static void AddRange<T>(this ItemCollection collection, IEnumerable<T> values) {
            foreach (var value in values) {
                collection.Add(value);
            }
        }
    }
}