using System;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Helpers.Essentials;
using MaterialComponents;

namespace AINotes.Helpers.PreferenceHelpers {
    public class IntegerPreference : Preference {
        private readonly int _defaultValue;
        private MDEntry _view;

        public IntegerPreference(string displayName, int defaultValue, Action onChanged=null, [CallerMemberName] string propertyName = "") : base(propertyName, displayName, onChanged) {
            _defaultValue = defaultValue;
        }
        
        private int GetValue() => UserPreferenceHelper.Get(PropertyName, _defaultValue);

        private void Save() {
            var success = int.TryParse(_view.Text, out var newValue);
            if (success) {
                UserPreferenceHelper.Set(PropertyName, newValue);
                OnChanged();
            } else {
                UserPreferenceHelper.Set(PropertyName, _defaultValue);
                _view.Text = _defaultValue.ToString();
            }
        }
        
        public override UIElement GetView() {
            if (_view != null) return _view;
            
            _view = new MDEntry {
                Text = GetValue().ToString(),
                // Keyboard = Keyboard.Numeric,
                // Regex = @"^[0-9]+$"
            };
            
            _view.LostFocus += OnLostFocus;

            // _view.Completed += (sender, args) => Save();
            
            return _view;
        }

        private void OnLostFocus(object sender, RoutedEventArgs args) {
            if (string.IsNullOrEmpty(_view.Text)) {
                _view.Text = GetValue().ToString();
            } else {
                Save();
            }
        }

        public static implicit operator int(IntegerPreference x) => x.GetValue();
    }
}