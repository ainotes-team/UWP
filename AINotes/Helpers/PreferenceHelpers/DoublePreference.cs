using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Helpers.Essentials;
using MaterialComponents;

namespace AINotes.Helpers.PreferenceHelpers {
    public class DoublePreference : Preference {
        private readonly double _defaultValue;
        private MDEntry _view;
        private readonly double? _minValue;
        private readonly double? _maxValue;

        public DoublePreference(string displayName, double defaultValue, Action onChanged=null, double? minValue = null, double? maxValue = null, [CallerMemberName] string propertyName = "") : base(propertyName, displayName, onChanged) {
            _defaultValue = defaultValue;
            _minValue = minValue;
            _maxValue = maxValue;
        }
        
        private double GetValue() => UserPreferenceHelper.Get(PropertyName, _defaultValue);

        private void Save() {
            var success = double.TryParse(_view.Text, out var newValue);
            if (success) {
                if (_minValue != null && newValue < _minValue) {
                    UserPreferenceHelper.Set(PropertyName, _defaultValue);
                    _view.Text = _defaultValue.ToString(CultureInfo.InvariantCulture);
                    return;
                }
                if (_maxValue != null && newValue > _maxValue) {
                    UserPreferenceHelper.Set(PropertyName, _defaultValue);
                    _view.Text = _defaultValue.ToString(CultureInfo.InvariantCulture);
                    return;
                }
                UserPreferenceHelper.Set(PropertyName, newValue);
                OnChanged();
            } else {
                UserPreferenceHelper.Set(PropertyName, _defaultValue);
                _view.Text = _defaultValue.ToString(CultureInfo.InvariantCulture);
            }
        }
        
        public override UIElement GetView() {
            if (_view != null) return _view;
            
            _view = new MDEntry {
                Text = GetValue().ToString(CultureInfo.InvariantCulture),
                // Keyboard = Keyboard.Numeric,
                // Regex = @"^[0-9\.]+$"
            };
            
            _view.LostFocus += (_, _) => {
                if (string.IsNullOrEmpty(_view.Text)) {
                    _view.Text = GetValue().ToString(CultureInfo.InvariantCulture);
                } else {
                    Save();
                }
            };

            // _view.Completed += (sender, args) => Save();
            
            return _view;
        }
        
        public static implicit operator double(DoublePreference x) => x.GetValue();
        public static implicit operator float(DoublePreference x) => (float) x.GetValue();
    }
}