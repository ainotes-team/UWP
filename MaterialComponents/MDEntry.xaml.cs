using System.Text.RegularExpressions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Helpers;
using Helpers.Extensions;

namespace MaterialComponents {
    public partial class MDEntry {
        public string RegexPattern { get; set; } = null;
        
        private readonly Brush _borderBrush = ColorCreator.FromHex("#DADCE0").ToBrush();
        private readonly Brush _focusBrush = ColorCreator.FromHex("#1A73E8").ToBrush();
        public static readonly Brush ErrorBrush = ColorCreator.FromHex("#D93025").ToBrush();

        private bool _error;
        public bool Error {
            get => _error;
            set {
                _error = value;
                if (_error) {
                    BorderBrush = ErrorBrush;
                    Foreground = ErrorBrush;
                } else
                {
                    Foreground = Theming.CurrentTheme.Text;
                    BorderBrush = _focused ? _focusBrush : _borderBrush;
                }
            }
        }

        private bool _isPasswordEntry;
        public bool IsPasswordEntry {
            get => _isPasswordEntry;
            set {
                _isPasswordEntry = value;
                FontFamily = _isPasswordEntry && Text.Length > 0 ? new FontFamily("/MaterialComponents/Assets/MDEntry/Password.ttf#Password") : FontFamily.XamlAutoFontFamily;
            }
        }
        
        private bool _focused;

        public MDEntry() : this(true) {}
        public MDEntry(bool hasBorder) {
            InitializeComponent();
            BorderThickness = new Thickness(hasBorder ? 1 : 0);
            
            BorderBrush = _borderBrush;
            Foreground = Theming.CurrentTheme.Text;
            PlaceholderForeground = ColorCreator.FromHex("#DADCE0").ToBrush();
            
            GettingFocus += (sender, args) => {
                if (args.Direction == FocusNavigationDirection.Next) {
                    Logger.Log("[CustomEntry]", "GettingFocus: FocusDirection Next from", args.OldFocusedElement);
                    // args.Cancel = true;
                }
                
                // selection color
                BorderBrush = _focusBrush;
                Foreground = Theming.CurrentTheme.Text;
                BorderThickness = new Thickness(hasBorder ? 1.5 : 0);

                _focused = true;
            };
            
            LosingFocus += (sender, args) => {
                BorderBrush = _borderBrush;
                Foreground = Theming.CurrentTheme.Text;
                BorderThickness = new Thickness(hasBorder ? 1 : 0);
                
                _focused = false;
            };

            TextChanged += (sender, args) => {
                IsPasswordEntry = IsPasswordEntry;
                CheckRegex();
            };

            ContextMenuOpening += OnContextMenuOpening;
        }
        
        
        private void CheckRegex() {
            if (RegexPattern == null) return;
            Error = !Regex.IsMatch(Text, RegexPattern);
        }

        public string Placeholder {
            // get => PlaceholderText;
            set => PlaceholderText = value;
        }

        private void OnContextMenuOpening(object sender, ContextMenuEventArgs args) {
            args.Handled = true;
        }
    }
}