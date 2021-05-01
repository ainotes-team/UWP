using System.Collections.Generic;
using MaterialComponents;

namespace AINotes.Controls.Sidebar {
    public interface ISidebarView {
        public IEnumerable<MDToolbarItem> ExtraButtons { get; }
    }
}