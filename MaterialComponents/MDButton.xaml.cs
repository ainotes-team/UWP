using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Helpers.Extensions;

namespace MaterialComponents {
    public enum MDButtonStyle {
        Primary,
        Secondary,
        Custom,
        Error
    }

    public partial class MDButton {
        private readonly Stack<Brush> _brushStack = new Stack<Brush>();
        
        private MDButtonStyle _buttonStyle;
        public MDButtonStyle ButtonStyle {
            get => _buttonStyle;
            set {
                _buttonStyle = value;
                switch (_buttonStyle) {
                    case MDButtonStyle.Primary:
                        Background = Theming.CurrentTheme.ButtonBackgroundPrimary;
                        Foreground = Theming.CurrentTheme.ButtonForegroundPrimary;
                        BorderBrush = Theming.CurrentTheme.ButtonBorderPrimary;
                        break;
                    case MDButtonStyle.Secondary:
                        Background = Theming.CurrentTheme.ButtonBackgroundSecondary;
                        Foreground = Theming.CurrentTheme.ButtonForegroundSecondary;
                        BorderBrush = Theming.CurrentTheme.ButtonBorderSecondary;
                        break;
                    case MDButtonStyle.Error:
                        Foreground = Colors.White.ToBrush();
                        Background = BorderBrush = ColorCreator.FromHex("#E81123").ToBrush();
                        _brushStack.Clear();
                        break;
                    case MDButtonStyle.Custom:
                        break;
                }
            }
        }

        private bool _isEnabled = true;
        public new bool IsEnabled {
            get {
                base.IsEnabled = _isEnabled;
                return _isEnabled;
            }
            set {
                base.IsEnabled = value;
                _isEnabled = value;
                if (value) {
                    ButtonStyle = ButtonStyle;
                } else {
                    Background = Theming.CurrentTheme.ButtonBackgroundDisabled;
                    Foreground = Theming.CurrentTheme.ButtonForegroundDisabled;
                    BorderBrush = Theming.CurrentTheme.ButtonBorderDisabled;
                }
            }
        }

        public string Text {
            get => (string) Content;
            set => Content = value;
        }

        public new Action Command;

        public MDButton() {
            InitializeComponent();
            
            ButtonStyle = MDButtonStyle.Primary;
            Theming.ThemeChanged += () => ButtonStyle = _buttonStyle;

            BorderThickness = new Thickness(1);
            CornerRadius = new CornerRadius(4);

            PointerEntered += OnPointerEntered;
            PointerPressed += OnPointerPressed;
            PointerReleased += OnPointerReleased;
            PointerExited += OnPointerExited;
            PointerCanceled += OnPointerCancelled;

            Click += (sender, args) => Command?.Invoke();
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e) {
            Lighten();
            base.OnPointerReleased(e);
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e) {
            Darken(0.125f);
            base.OnPointerPressed(e);
        }

        private void Darken(float amount) {
            _brushStack.Push(Background);
            if (!(Background is SolidColorBrush solidBackgroundBrush)) throw new InvalidCastException("MDButton.Background must be of type SolidColorBrush.");
            Background = solidBackgroundBrush.Color.Merge(Colors.Black, amount).ToBrush();
        }

        private void Lighten() {
            if (_brushStack.Count == 0) return;
            Background = _brushStack.Pop();
        }

        private void Reset() {
            while (_brushStack.Count > 0) {
                Background = _brushStack.Pop();
            }
        }

        private void OnPointerEntered(object sender, PointerRoutedEventArgs args) => Darken(0.075f);
        private void OnPointerPressed(object sender, PointerRoutedEventArgs args) => Darken(0.125f);
        private void OnPointerReleased(object sender, PointerRoutedEventArgs args) => Lighten();
        private void OnPointerExited(object sender, PointerRoutedEventArgs args) => Reset();
        private void OnPointerCancelled(object sender, PointerRoutedEventArgs args) => Reset();
    }
}