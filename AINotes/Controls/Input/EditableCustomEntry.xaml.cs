using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace AINotes.Controls.Input {
    public partial class EditableCustomEntry {
        public bool Editable { get; set; }
        public event RoutedEventHandler EditingStarting;
        public event RoutedEventHandler EditingStarted;
        public event RoutedEventHandler EditingStopping;
        public event RoutedEventHandler EditingStopped;

        public EditableCustomEntry() {
            SetReadonlyStyle();

            GettingFocus += OnGettingFocus;
            LosingFocus += OnLosingFocus;
        }

        private void OnGettingFocus(UIElement sender, GettingFocusEventArgs args) {
            if (!Editable) {
                args.Cancel = true;
            } else {
                EditingStarting?.Invoke(this, null);
            }
        }

        private void OnLosingFocus(UIElement sender, LosingFocusEventArgs args) {
            EditingStopping?.Invoke(this, null);
        }

        protected override void OnTapped(TappedRoutedEventArgs e) {
            if (Editable) {
                IsReadOnly = false;
                SetEditingStyle();
            }
            
            base.OnTapped(e);
        }

        protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e) {
            if (Editable) {
                IsReadOnly = false;
                SetEditingStyle();
            }

            base.OnDoubleTapped(e);
        }

        protected override void OnLostFocus(RoutedEventArgs e) {
            IsReadOnly = true;
            SetReadonlyStyle();
            base.OnLostFocus(e);
            EditingStopped?.Invoke(this, null);
        }

        public void SetReadonlyStyle() {
            if (BorderBrush != null) BorderBrush.Opacity = 0;
            if (Background != null) Background.Opacity = 0;
        }

        public void SetEditingStyle() {
            if (BorderBrush != null) BorderBrush.Opacity = 1;
            if (Background != null) Background.Opacity = 1;
            EditingStarted?.Invoke(this, null);
        }
    }
}