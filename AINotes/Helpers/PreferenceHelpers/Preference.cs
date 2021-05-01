using System;
using Windows.UI.Xaml;

namespace AINotes.Helpers.PreferenceHelpers {
    public abstract class Preference {
        public string GetDisplayName() => _displayName;
        public abstract UIElement GetView();

        public event Action Changed;
        private readonly Action _onChangedAction;
        protected void OnChanged() {
            _onChangedAction?.Invoke();
            Changed?.Invoke();
        }

        protected readonly string PropertyName;
        private readonly string _displayName;
        
        protected Preference(string propertyName, string displayName, Action onChanged) {
            if (string.IsNullOrEmpty(propertyName)) {
                throw new ArgumentException("propertyName can not be null or empty");
            }

            PropertyName = propertyName;
            _displayName = displayName;
            _onChangedAction = onChanged;
        }
        
    }
}