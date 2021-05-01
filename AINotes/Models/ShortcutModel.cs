using System;
using System.Collections.Generic;

namespace AINotes.Models {
    public class ShortcutModel {
        private Func<List<string>> GetKeys { get; }
        public IEnumerable<string> Keys => GetKeys?.Invoke();
        public string Identifier { get; }
        public Action Action { get; }
        public bool ReturnAfter { get; }

        public ShortcutModel(Func<List<string>> getKeys, string identifier, Action action, bool returnAfter=false) {
            GetKeys = getKeys;
            Identifier = identifier;
            Action = action;
            ReturnAfter = returnAfter;
        }
    }
}