using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using MaterialComponents;

namespace AINotes.Helpers.PreferenceHelpers {
    public class InvocationPreference : Preference {
        private readonly Action _callback;
        private Button _view;
        
        public InvocationPreference(string displayName, Action callback) : base("invocation", displayName, null) => _callback = callback;
        
        public override UIElement GetView() => _view ??= new MDButton {
            Command = _callback,
            Text = GetDisplayName(),
            Width = 200,
            ButtonStyle = MDButtonStyle.Secondary
        };
    }
}