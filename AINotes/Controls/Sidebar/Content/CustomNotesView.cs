using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using MaterialComponents;

namespace AINotes.Controls.Sidebar.Content {
    public class CustomNotesView : StackPanel, ISidebarView {
        public IEnumerable<MDToolbarItem> ExtraButtons { get; } = new MDToolbarItem[] { };
    }
}