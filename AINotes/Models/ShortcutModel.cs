using System;
using System.Collections.Generic;

namespace AINotes.Models {
    public class ShortcutModel {
        private Func<List<string>> GetKeys { get; }
        
        /// <summary>The Keys Function must return all Keys necessary for the shortcut.</summary>
        public IEnumerable<string> Keys => GetKeys?.Invoke();
        
        /// <summary>Shortcut ID / Name</summary>
        public string Identifier { get; }
        
        /// <summary>The Action to take when the shortcut is pressed. Must return true if the shortcut was handled, false otherwise.</summary>
        public Func<bool> Action { get; }
        public bool ReturnAfter { get; }

        /// <summary>The ShortcutModel provides a simple way to register application-wide shortcuts.</summary>
        /// <param name="getKeys">getKeys must return a List of strings defining which keys need to be pressed</param>
        /// <param name="identifier">Shortcut ID / Name</param>
        /// <param name="action">The action to take when triggered. Must return true if handled, false otherwise.</param>
        /// <param name="returnAfter">Should we keep looking for matching shortcuts?</param>
        public ShortcutModel(Func<List<string>> getKeys, string identifier, Func<bool> action, bool returnAfter=false) {
            GetKeys = getKeys;
            Identifier = identifier;
            Action = action;
            ReturnAfter = returnAfter;
        }
    }
}