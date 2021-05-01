using System;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Helpers.Essentials;
using MaterialComponents;

namespace AINotes.Helpers.PreferenceHelpers {
    public class StringListPreference : Preference {
        private string[] _options;
        private readonly Func<string[]> _getOptions;
        private string[] Options {
            get => _options ?? _getOptions();
            set => _options = value;
        }
        
        private readonly string _defaultValue;
        private MDPicker _view;

        private string GetValue() => UserPreferenceHelper.Get(PropertyName, _defaultValue) ?? _defaultValue;

        public override UIElement GetView() {
            if (_view != null) return _view;

            _view = new MDPicker {
                ItemsSource = Options
            };

            if (_view.Items != null) {
                foreach (var viewItem in _view.Items) {
                    if (!ReferenceEquals(viewItem, GetValue())) continue;
                    _view.SelectedItem = viewItem;
                    break;
                }
            }

            _view.SelectionChanged += OnSelectionChanged;

            return _view;
        }

        private void OnSelectionChanged(object o, SelectionChangedEventArgs selectionChangedEventArgs) {
            UserPreferenceHelper.Set(PropertyName, (string) _view.SelectedItem);
            OnChanged();
        }

        public StringListPreference(string displayName, string[] options, string defaultValue="", Action onChanged=null, [CallerMemberName] string propertyName = "") : base(propertyName, displayName, onChanged) {
            Options = options;
            _defaultValue = defaultValue;
        }
        
        public StringListPreference(string displayName, Func<string[]> getOptions, string defaultValue="", Action onChanged=null, [CallerMemberName] string propertyName = "") : base(propertyName, displayName, onChanged) {
            _getOptions = getOptions;
            _defaultValue = defaultValue;
        }
        
        public static implicit operator string(StringListPreference x) => x.GetValue();
    }
}