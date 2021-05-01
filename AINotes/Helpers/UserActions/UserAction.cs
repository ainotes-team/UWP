using System;
using System.Collections.Generic;

namespace AINotes.Helpers.UserActions {
    public class UserAction {
        private readonly Dictionary<string, object> _actionStorage;
        private readonly Action<Dictionary<string, object>> _redoAction;
        private readonly Action<Dictionary<string, object>> _undoAction;

        public UserAction(Action<Dictionary<string, object>> undoAction, Action<Dictionary<string, object>> redoAction, Dictionary<string, object> actionStorage = null) {
            _actionStorage = actionStorage ?? new Dictionary<string, object>();
            _undoAction = undoAction;
            _redoAction = redoAction;
        }

        public void Undo() {
            UserActionManager.UndoToolbarItem.IsEnabled = false;
            UserActionManager.RedoToolbarItem.IsEnabled = false;
            _undoAction(_actionStorage);
            UserActionManager.UndoToolbarItem.IsEnabled = true;
            UserActionManager.RedoToolbarItem.IsEnabled = true;
        }

        public void Redo() {
            UserActionManager.UndoToolbarItem.IsEnabled = false;
            UserActionManager.RedoToolbarItem.IsEnabled = false;
            _redoAction(_actionStorage);
            UserActionManager.UndoToolbarItem.IsEnabled = true;
            UserActionManager.RedoToolbarItem.IsEnabled = true;
        }
    }
}