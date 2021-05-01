using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Helpers.Lists {
    public class ExtendedUIElementCollection : IEnumerable<UIElement> {
        private readonly UIElementCollection _uiElementCollection;
        
        public ExtendedUIElementCollection(UIElementCollection uiElementCollection) {
            _uiElementCollection = uiElementCollection;
        }

        public event Action<UIElement> UIElementAdded;
        public event Action<UIElement> UIElementRemoved;
        
        public void Add(UIElement item) {
            _uiElementCollection.Add(item);
            UIElementAdded?.Invoke(item);
        }

        public bool Contains(UIElement item) {
            return _uiElementCollection.Contains(item);
        }

        public void Remove(UIElement item) {
            _uiElementCollection.Remove(item);
            UIElementRemoved?.Invoke(item);
        }

        public IEnumerator<UIElement> GetEnumerator() {
            return _uiElementCollection.ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}