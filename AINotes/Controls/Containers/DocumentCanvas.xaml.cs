using System;
using Windows.UI.Xaml;
using Helpers.Lists;

namespace AINotes.Controls.Containers {
    public partial class DocumentCanvas {
        public event Action<UIElement> ChildAdded;
        public event Action<UIElement> ChildRemoved;

        public new readonly ExtendedUIElementCollection Children;

        public DocumentCanvas() {
            Children = new ExtendedUIElementCollection(base.Children);
            Children.UIElementAdded += itm => ChildAdded?.Invoke(itm);
            Children.UIElementRemoved += itm => ChildRemoved?.Invoke(itm);
        }
    }
}