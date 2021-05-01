using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AINotes.Controls.Input;
using Helpers.Essentials;

namespace AINotes.Helpers.PreferenceHelpers {
    public class ShortcutPreference : Preference {
        private readonly List<string> _defaultValue;
        private CustomShortcutEntry _view;

        private const string Separator = " + ";
        
        private string GetDefaultString() => string.Join(Separator, _defaultValue);
        private string GetString() => string.Join(Separator, GetValue());
        private List<string> GetValue() => UserPreferenceHelper.Get(PropertyName, GetDefaultString()).Split(new[] {Separator}, StringSplitOptions.None).ToList();
        
        public override UIElement GetView() {
            if (_view != null) return _view;
            
            UserPreferenceHelper.Set(PropertyName, null);
            
            _view = new CustomShortcutEntry {
                Text = GetString()
            };
            
            _view.TextChanged += OnTextChanged;
            
            return _view;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs args) {
            if (_view.Text.Replace(" ", "").EndsWith("+")) return;
            UserPreferenceHelper.Set(PropertyName, _view.Text);
            OnChanged();
        }

        public ShortcutPreference(string displayName, List<string> defaultValue, Action onChanged=null, [CallerMemberName] string propertyName = "") : base(propertyName, displayName, onChanged) {
            _defaultValue = defaultValue;
        }
        
        public static implicit operator List<string>(ShortcutPreference x) => x.GetValue();
    }
}