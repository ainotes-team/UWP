using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Helpers.Extensions {
    public static class DependencyObjectExtensions {
        public static List<object> ListChildren(this DependencyObject element, bool recursive = true, List<object> results = null) {
            if (results == null) results = new List<object>();

            var count = VisualTreeHelper.GetChildrenCount(element);
            for (var i = 0; i < count; i++) {
                var current = VisualTreeHelper.GetChild(element, i);
                results.Add(current);
                if (recursive) ListChildren(current, true, results);
            }

            return results;
        }
        
        public static DependencyObject GetParent(this DependencyObject obj) => VisualTreeHelper.GetParent(obj);
        public static DependencyObject GetParent(this DependencyObject obj, int idx) {
            for (var i = 0; i < idx; i++) {
                obj = GetParent(obj);
            }

            return obj;
        }
    }
}