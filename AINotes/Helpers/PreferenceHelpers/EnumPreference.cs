using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Helpers.Essentials;
using MaterialComponents;

namespace AINotes.Helpers.PreferenceHelpers {
    public class EnumPreference<T> : Preference where T : Enum {
        private readonly T[] _options;
        private readonly List<T> _except = new List<T>();
        private readonly T _defaultValue;
        private MDPicker _view;

        private T GetValue() => (T) Enum.Parse(typeof(T), UserPreferenceHelper.Get(PropertyName, _defaultValue.ToString()));

        public override UIElement GetView() {
            if (_view != null) return _view;

            _view = new MDPicker {
                ItemsSource = _options.Where(s => !_except.Contains(s)).Select(s => s.ToString()).ToList(),
                SelectedItem = GetValue().ToString()
            };

            _view.SelectionChanged += (_, _) => {
                UserPreferenceHelper.Set(PropertyName, _view.SelectedItem?.ToString());
                OnChanged();
            };

            return _view;
        }

        public EnumPreference(string displayName, T defaultValue = default, List<T> except = null, Action onChanged = null, [CallerMemberName] string propertyName = "") : base(propertyName, displayName, onChanged) {
            _defaultValue = defaultValue;
            if (except != null) _except = except;
            _options = (T[]) Enum.GetValues(typeof(T));
        }

        public static implicit operator T(EnumPreference<T> x) => x.GetValue();
    }
}