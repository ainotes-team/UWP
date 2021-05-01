using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Input;
using Helpers;
using Helpers.Extensions;

namespace AINotes.Controls.Toolbar {
    public partial class MDToolbar {
        private const int ToolbarHeight = 48;

        public UIElementCollection PrimaryToolbarChildren => MainToolbar?.Children;
        public UIElementCollection SecondaryToolbarChildren => SecondaryToolbar?.Children;

        public string Title {
            get => TitleLabel.Text;
            set => TitleLabel.Text = value;
        }

        private Action _backCallback;
        public Action BackCallback {
            get => _backCallback;
            set {
                _backCallback = value;
                if (value == null) {
                    BackButton.Visibility = Visibility.Collapsed;
                    BackButtonColumn.Width = new GridLength(0, GridUnitType.Pixel);
                } else {
                    BackButton.Visibility = Visibility.Visible;
                    BackButtonColumn.Width = new GridLength(1, GridUnitType.Auto);
                }
            }
        }

        public MDToolbar() {
            InitializeComponent();

            Background = Colors.White.ToBrush();
            BorderBrush = Color.FromArgb(255, 218, 220, 224).ToBrush();
            BorderThickness = new Thickness(0, 0, 0, 1);

            Window.Current.SizeChanged += OnWindowSizeChanged;
        }

        protected void OnTitleContextRequested(object sender, ContextRequestedEventArgs e) {
            Logger.Log("[CustomToolbar]", "OnTitleContextRequested");
        }

        private void OnBackButtonReleased(object sender, EventArgs e) {
            _backCallback?.Invoke();
        }

        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e) {
            var totalToolbarMinSize = BackButton.ActualWidth + TitleLabel.ActualWidth + SecondaryToolbar.ActualWidth + MainToolbar.ActualWidth + 20;
            Logger.Log("[CustomToolbar]", "OnWindowSizeChanged", e.Size.Width, "--", totalToolbarMinSize);

            var missingSpace = totalToolbarMinSize - e.Size.Width;

            if (missingSpace < 0) {
                if (TitleLabel.MaxWidth < 250) {
                    TitleLabel.MaxWidth = (-missingSpace).Clamp(16, 250);
                }
            }

            // ReSharper disable once InvertIf
            if (missingSpace > 0) {
                // resize title
                if (TitleLabel.MaxWidth > 16) {
                    var newMaxWidth = (TitleLabel.MaxWidth - missingSpace).Clamp(16, 250);
                    missingSpace -= TitleLabel.ActualWidth - newMaxWidth;
                    TitleLabel.MaxWidth = newMaxWidth;
                }

                if (missingSpace > 0) {
                    Logger.Log("[CustomToolbar]", "OnWindowSizeChanged: missingSpace > 0 (TODO)");
                }
            }
        }
    }
}