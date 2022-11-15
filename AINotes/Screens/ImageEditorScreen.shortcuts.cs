using AINotes.Controls.Popups;
using AINotes.Helpers;
using AINotes.Models;

namespace AINotes.Screens {
    public partial class ImageEditorScreen {
        // ReSharper disable once ConvertIfStatementToReturnStatement
        private bool ShouldExecute() {
            if (App.Page.Content != this) return false;
            if (PopupNavigation.CurrentPopup != null) return false;

            return true;
        }
        
        public void RegisterShortcuts() {
            Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.CancelShortcut, "closeImageEditor", CloseShortcut));
        }

        private bool CloseShortcut() {
            if (!ShouldExecute()) return false;
            App.Page.GoBack();
            return true;
        }
        
    }
}