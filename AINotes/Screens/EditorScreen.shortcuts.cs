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

        private bool MoveToForegroundShortcut() {
            if (!ShouldExecute()) return false;

            var documentComponents = App.EditorScreen.GetDocumentComponents();
            if (documentComponents.Count == 0) return false;
            var currentMaxZIndex = documentComponents.Max(Canvas.GetZIndex);
            
            foreach (var pl in App.EditorScreen.GetDocumentComponents().Where(pl => pl.IsSelected)) {
                Canvas.SetZIndex(pl, currentMaxZIndex + 1);
            }

            return true;
        }

        private bool MoveToBackgroundShortcut() {
            if (!ShouldExecute()) return false;

            var currentMinZIndex = App.EditorScreen.GetDocumentComponents().Min(Canvas.GetZIndex);
            
            foreach (var pl in App.EditorScreen.GetDocumentComponents().Where(pl => pl.IsSelected)) {
                Canvas.SetZIndex(pl, currentMinZIndex - 1);
            }

            return true;
        }

        private bool ResetZoomShortcut() {
            if (!ShouldExecute()) return false;

            App.EditorScreen.Scroll.ChangeView(0f, 0f, 1.0f);
            return true;
        }

        private bool DeleteShortcut() {
            if (!ShouldExecute()) return false;

            // check if a selection exists
            if (App.EditorScreen._selectionComponent == null) {
                if (SelectedContent.Count != 1) return false;
                SelectedContent[0].SetDeleted(true);
                ContentChanged = true;
                return false;
            }

            // delete the selection component which deletes its contents
            App.EditorScreen._selectionComponent.Delete();

            // the document has changed
            ContentChanged = true;
            return true;
        }

        // select all elements
        private bool SelectAllShortcut() {
            if (!ShouldExecute()) return false;
            
            InkCanvas.SelectAll();
            return true;
        }
        
        // Undo
        private bool UndoShortcut() {
            if (!ShouldExecute()) return false;
            
            UserActionManager.Undo();
            return true;
        }
        
        // Redo
        private bool RedoShortcut() {
            if (!ShouldExecute()) return false;
            
            UserActionManager.Redo();
            return true;
        }

        private bool InvertSelectionShortcut() {
            if (!ShouldExecute()) return false;
            
            InkCanvas.InvertSelection();
            foreach (var component in App.EditorScreen.GetDocumentComponents().ToList()) {
                component.IsSelected = !component.IsSelected;
            }
            return true;
        }

        private bool CopyShortcut() {
            if (!ShouldExecute()) return false;
            _selectionComponent.Copy();
            return true;
        }

        private bool CutShortcut() {
            if (!ShouldExecute()) return false;
            _selectionComponent.Cut();
            return true;
        }

        private bool PasteShortcut() {
            if (!ShouldExecute()) return false;
            var (touchX, touchY) = _lastPointerPosition;
            var resultPoint = new Point(touchX, touchY);
            ClipboardManager.Paste(resultPoint);
            return true;
        }
    }
}