using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using AINotes.Components.Implementations;
using AINotes.Helpers;
using AINotes.Helpers.UserActions;
using Helpers;
using Helpers.Extensions;
using AINotes.Models;

namespace AINotes.Screens {
    public partial class EditorScreen {
        // ReSharper disable once ConvertIfStatementToReturnStatement
        private bool ShouldExecute() {
            // check if the EditorScreen is active
            if (App.Page.Content != this) return false;
            
            // check if a HandwritingComponent is active
            if (SelectedContent.Count == 1 && SelectedContent[0].GetType() == typeof(TextComponent)) return false;

            return true;
        }

        // load all shortcuts
        private async void RegisterShortcuts() {
            Logger.Log("[EditorScreen]", "-> LoadShortcuts", logLevel: LogLevel.Verbose);

            await Task.Run(() => {
                Logger.Log("[EditorScreen]", "LoadShortcuts -> Task", logLevel: LogLevel.Verbose);
                Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.DeleteShortcut, "edt_delete", DeleteShortcut));
                Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.SelectAllShortcut, "edt_selectAll", SelectAllShortcut));
                Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.UndoShortcut, "edt_undo", UndoShortcut));
                Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.RedoShortcut, "edt_redo", RedoShortcut));
                Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.InvertSelectionShortcut, "edt_invert", InvertSelectionShortcut));
                Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.CopyShortcut, "edt_copy", CopyShortcut));
                Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.CutShortcut, "edt_cut", CutShortcut));
                Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.PasteShortcut, "edt_paste", PasteShortcut));
                Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.MoveToForegroundShortcut, "edt_toFg", MoveToForegroundShortcut));
                Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.MoveToBackgroundShortcut, "edt_toBg", MoveToBackgroundShortcut));
                Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.ResetZoomShortcut, "edt_resetZoom", ResetZoomShortcut));
                Logger.Log("[EditorScreen]", "LoadShortcuts <- Task", logLevel: LogLevel.Verbose);
            });

            Logger.Log("[EditorScreen]", "<- LoadShortcuts", logLevel: LogLevel.Verbose);
        }

        private void MoveToForegroundShortcut() {
            if (!ShouldExecute()) return;

            var documentComponents = App.EditorScreen.GetDocumentComponents();
            if (documentComponents.Count == 0) return;
            var currentMaxZIndex = documentComponents.Max(Canvas.GetZIndex);
            
            foreach (var pl in App.EditorScreen.GetDocumentComponents().Where(pl => pl.IsSelected)) {
                Canvas.SetZIndex(pl, currentMaxZIndex + 1);
            }
        }

        private void MoveToBackgroundShortcut() {
            if (!ShouldExecute()) return;

            var currentMinZIndex = App.EditorScreen.GetDocumentComponents().Min(Canvas.GetZIndex);
            
            foreach (var pl in App.EditorScreen.GetDocumentComponents().Where(pl => pl.IsSelected)) {
                Canvas.SetZIndex(pl, currentMinZIndex - 1);
            }
        }

        private void ResetZoomShortcut() {
            if (!ShouldExecute()) return;

            App.EditorScreen.Scroll.ChangeView(0f, 0f, 1.0f);
        }

        private void DeleteShortcut() {
            if (!ShouldExecute()) return;

            // check if a selection exists
            if (App.EditorScreen._selectionComponent == null) {
                if (SelectedContent.Count != 1) return;
                SelectedContent[0].SetDeleted(true);
                ContentChanged = true;
                return;
            }

            // delete the selection component which deletes its contents
            App.EditorScreen._selectionComponent.Delete();

            // the document has changed
            ContentChanged = true;
        }

        // select all elements
        private void SelectAllShortcut() {
            if (!ShouldExecute()) return;
            
            InkCanvas.SelectAll();
        }
        
        // Undo
        private void UndoShortcut() {
            if (!ShouldExecute()) return;
            
            UserActionManager.Undo();
        }
        
        // Redo
        private void RedoShortcut() {
            if (!ShouldExecute()) return;
            
            UserActionManager.Redo();
        }

        private void InvertSelectionShortcut() {
            if (!ShouldExecute()) return;
            
            InkCanvas.InvertSelection();
            foreach (var component in App.EditorScreen.GetDocumentComponents().ToList()) {
                component.IsSelected = !component.IsSelected;
            }
        }

        private void CopyShortcut() {
            if (!ShouldExecute()) return;
            _selectionComponent.Copy();
        }

        private void CutShortcut() {
            if (!ShouldExecute()) return;
            _selectionComponent.Cut();
        }

        private void PasteShortcut() {
            if (!ShouldExecute()) return;
            var (touchX, touchY) = _lastPointerPosition;
            var resultPoint = new Point(touchX, touchY);
            ClipboardManager.Paste(resultPoint);
        }
    }
}