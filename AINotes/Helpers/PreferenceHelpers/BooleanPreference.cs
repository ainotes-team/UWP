using System;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Helpers.Essentials;

namespace AINotes.Helpers.PreferenceHelpers {
    public sealed class BooleanPreference : Preference {
        private readonly bool _defaultValue;
        private ToggleSwitch _view;

        private bool GetValue() => UserPreferenceHelper.Get(PropertyName, _defaultValue);

        public override UIElement GetView() {
            if (_view != null) return _view;
            
            _view = new ToggleSwitch {
                IsOn = GetValue()
            };
            _view.Toggled += OnToggled;

            return _view;
        }
        
        public BooleanPreference(string displayName, bool defaultValue=false, Action onChanged=null, [CallerMemberName] string propertyName = "") : base(propertyName, displayName, onChanged) {
            _defaultValue = defaultValue;
        }

        private void OnToggled(object sender, RoutedEventArgs args) {
            UserPreferenceHelper.Set(PropertyName, _view.IsOn);
            OnChanged();
        }


        public static implicit operator bool(BooleanPreference x) => x.GetValue();
    }
}