using AINotes.Controls.Popups;
using AINotes.Helpers;
using AINotes.Models;

namespace AINotes.Screens {
    public partial class CameraScreen {
        // ReSharper disable once ConvertIfStatementToReturnStatement
        private bool ShouldExecute() {
            if (App.Page.Content != this) return false;
            if (PopupNavigation.CurrentPopup != null) return false;

            return true;
        }
        
        public void RegisterShortcuts() {
            Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.CancelShortcut, "closeCameraScreen", CloseShortcut));
        }

        private void CloseShortcut() {
            if (!ShouldExecute()) return;
            App.Page.GoBack();
        }
    }
}