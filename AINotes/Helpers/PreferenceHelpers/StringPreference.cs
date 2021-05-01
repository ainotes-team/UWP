using System;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Helpers.Essentials;
using MaterialComponents;

namespace AINotes.Helpers.PreferenceHelpers {
    public sealed class StringPreference : Preference {
        private readonly string _defaultValue;
        private readonly string _hint;
        private MDEntry _view;

        private string GetValue() => UserPreferenceHelper.Get(PropertyName, _defaultValue);
        
        public override UIElement GetView() {
            if (_view != null) return _view;
            
            _view = new MDEntry {
                Text = GetValue(),
                PlaceholderText = _hint ?? "",
            };
            
            _view.TextChanged += OnTextChanged;
            _view.LostFocus += OnLostFocus;
            
            return _view;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs args) {
            UserPreferenceHelper.Set(PropertyName, _view.Text);
        }

        private void OnLostFocus(object sender, RoutedEventArgs args) {
            OnChanged();
        }

        public StringPreference(string displayName, string defaultValue="", Action onChanged=null, string hint=null, [CallerMemberName] string propertyName = "") : base(propertyName, displayName, onChanged) {
            _defaultValue = defaultValue;
            _hint = hint;
        }
        
        public static implicit operator string(StringPreference x) => x.GetValue();
    }
}