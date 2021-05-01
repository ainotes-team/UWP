using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using AINotes.Models;
using Helpers;
using Helpers.Lists;

namespace AINotes.Helpers {
    public static class Shortcuts {
        public static event Action EnterPressed;
        
        public static readonly List<VirtualKey> PressedKeys = new List<VirtualKey>();
        public static readonly List<ShortcutModel> RegisteredShortcuts = new List<ShortcutModel>(); 
        public static readonly List<ShortcutModel> RegisteredSequences = new List<ShortcutModel>();

        public static readonly ExtendedArrayList LastKeys = new ExtendedArrayList {
            Limit = 25,
        };

        static Shortcuts() {
            Window.Current.Content.PreviewKeyDown += OnKeyDown;
            Window.Current.Content.PreviewKeyUp += OnKeyUp;

            Window.Current.CoreWindow.Activated += OnActivated;
        }
        
        private static void OnKeyDown(object o, KeyRoutedEventArgs args) {
            if (PressedKeys.Contains(args.Key)) return;
            // Logger.Log("[Shortcuts]", "KeyDown:", args.Key, "|", PressedKeys.ToFString());
            if (args.Key == VirtualKey.Enter) EnterPressed?.Invoke();
            
            PressedKeys.Add(args.Key);
            LastKeys.Add(args.Key);

            // shortcut check
            foreach (var sM in RegisteredShortcuts.Select(sM => new {sM, equal = sM.Keys.All(keyString => PressedKeys.Contains((VirtualKey) Enum.Parse(typeof(VirtualKey), keyString)))}).Where(t => t.equal).Select(t => t.sM)) {
                Logger.Log("[Shortcuts]", "Executing Shortcut", sM.Identifier);
                sM.Action?.Invoke();
                args.Handled = true;
                if (sM.ReturnAfter) break;
            }

            // sequence check
            var lastKeysString = string.Join(" ", LastKeys.ToArray().Select(itm => ((VirtualKey) itm).ToString())).ToLowerInvariant();
            foreach (var (sqKeyString, sq) in RegisteredSequences.Select(itm => (string.Join(" ", itm.Keys).ToLowerInvariant(), itm))) {
                if (!lastKeysString.EndsWith(sqKeyString)) continue;
                Logger.Log("[Shortcuts]", "Executing Sequence", sq.Identifier);
                sq.Action?.Invoke();
                args.Handled = true;
                if (sq.ReturnAfter) break;
            }
        }

        private static void OnKeyUp(object o, KeyRoutedEventArgs args) {
            if (!PressedKeys.Contains(args.Key)) return;
            // Logger.Log("[Shortcuts]", "KeyUp:", args.Key, "|", PressedKeys.ToFString());
            PressedKeys.Remove(args.Key);
        }

        private static void OnActivated(CoreWindow sender, WindowActivatedEventArgs args) {
            if (args.WindowActivationState == CoreWindowActivationState.Deactivated) {
                PressedKeys.Clear();
            }
        }

        public static void AddShortcut(ShortcutModel shortcutModel) {
            RegisteredShortcuts.Add(shortcutModel);
        }

        public static void AddSequence(ShortcutModel shortcutModel) {
            RegisteredSequences.Add(shortcutModel);
        }
    }
}