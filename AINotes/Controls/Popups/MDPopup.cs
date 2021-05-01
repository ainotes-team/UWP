using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;
using Helpers.Extensions;

namespace AINotes.Controls.Popups {
    public class MDPopup : ContentDialog {
        public bool CloseWhenBackgroundIsClicked { get; set; } = true;
        private Rectangle _backgroundRectangle;

        private Thickness? _borderBrushThickness;
        public Thickness? BorderBrushThickness {
            set {
                _borderBrushThickness = value;
                if (_borders == null) return;
                foreach (var border in _borders) {
                    border.BorderThickness = value ?? new Thickness(0);
                }
            }
        }

        private List<Border> _borders;

        public MDPopup() {
            Transitions = new TransitionCollection();
            ContentTransitions = new TransitionCollection();
            
            Closed += OnClosed;

            Loading += OnLoading;
            
            Loaded += OnLoaded;
        }

        private void OnLoading(FrameworkElement sender, object args) {
            var popups = VisualTreeHelper.GetOpenPopups(Window.Current);
            foreach (var p in popups) {
                p.ChildTransitions = new TransitionCollection();
                p.Transitions = new TransitionCollection();

                if (!(p.Child is Rectangle r)) continue;
                _backgroundRectangle = r;
                _backgroundRectangle.Tapped += OnBackgroundTapped;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs args) {
            _borders = this.ListChildren().ToArray().Where(itm => itm is Border b && b.Name == "BackgroundElement").Cast<Border>().ToList();
            foreach (var b in _borders) {
                b.Shadow = null;
                b.BorderBrush = Configuration.Theme.CardBorder;
                b.CornerRadius = new CornerRadius(5);
                if (_borderBrushThickness != null) {
                    b.BorderThickness = (Thickness) _borderBrushThickness;
                }
            }
        }

        private void OnClosed(ContentDialog s, ContentDialogClosedEventArgs e) {
            if (PopupNavigation.CurrentPopup == this) {
                PopupNavigation.CurrentPopup = null;
            }
        }

        private void OnBackgroundTapped(object sender, TappedRoutedEventArgs e) {
            if (CloseWhenBackgroundIsClicked) Hide();
            _backgroundRectangle.Tapped -= OnBackgroundTapped;
        }

        public void Show() {
            CloseCurrentPopup();
            PopupNavigation.CurrentPopup = this;
            ShowAsync();
        }

        public static void CloseCurrentPopup() => PopupNavigation.CloseCurrentPopup();
    }
}