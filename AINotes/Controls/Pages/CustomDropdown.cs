using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using AINotes.Helpers.Extensions;
using Helpers;
using Helpers.Controls;
using Helpers.Essentials;
using Helpers.Extensions;
using MaterialComponents;
using Point = Windows.Foundation.Point;

namespace AINotes.Controls.Pages {
    public static class CustomDropdown {
        public static bool DropdownProtection { get; set; }
        public static event Action DropdownClosed;
        
        private static Frame _currentDropdown;

        public static Frame CreateDropdown(IEnumerable<CustomDropdownViewTemplate> dropdownItems, EventHandler<WTouchEventArgs> touchHandler = null) {
            var dropdownStack = new StackPanel {
                Spacing = 0
            };

            foreach (var item in dropdownItems) {
                try {
                    dropdownStack.Children.Add(item);
                } catch (Exception) {
                    // ignore
                }
            }

            var dropdown = new CustomFrame {
                CornerRadius = new CornerRadius(4),
                Background = Configuration.Theme.Background,
                BorderBrush = Configuration.Theme.CardBorder,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(0, 7, 0, 7),
                Margin = new Thickness(0),
                Content = dropdownStack,
                Name = "CurrentDropdown"
            };

            if (touchHandler != null) {
                dropdown.Touch += touchHandler;
            }
            
            return dropdown;
        }
        public static void ShowDropdown(IEnumerable<CustomDropdownViewTemplate> dropdownItems, FrameworkElement anchor, int minWidth = 250) {
            var (x, y) = anchor.GetAbsoluteCoordinates();
            Logger.Log(y, "=>", anchor.RenderSize.Height);
            ShowDropdown(dropdownItems, new Point(x, y + anchor.RenderSize.Height), anchor, minWidth);
        }
        
        public static void ShowDropdown(IEnumerable<CustomDropdownViewTemplate> dropdownItems, Point position, FrameworkElement anchor = null, int minWidth = 250) {
            DropdownProtection = true;
            CloseDropdown();

            var (x, y) = position;
            var dropdown = CreateDropdown(dropdownItems);
            dropdown.Measure(new Size(int.MaxValue, int.MaxValue));

            // check width
            dropdown.Width = Math.Max(minWidth, dropdown.DesiredSize.Width);
            if (x + dropdown.Width > Window.Current.Bounds.Width) {
                x -= dropdown.Width - anchor?.ActualWidth ?? 0;
            } else if (anchor != null) {
                x -= anchor.ActualWidth;
            }
            
            // check height
            var height = dropdown.DesiredSize.Height;
            if (y + height > Window.Current.Bounds.Height) {
                y -= y + height - Window.Current.Bounds.Height;
            }
            
            _currentDropdown = dropdown;
            App.Page.AbsoluteOverlay.AddChild(_currentDropdown, new Point(x, y));
        }

        public static void CloseDropdown([CallerMemberName] string callerName=null, [CallerLineNumber] int callerLine=0) {
            if (_currentDropdown == null) return;
            Logger.Log("[CustomContentPage]", "CloseDropdown <=", callerName, "@", callerLine);
            App.Page.AbsoluteOverlay.Children.Remove(_currentDropdown);
            _currentDropdown = null;
            DropdownClosed?.Invoke();
        }
    }
    
    public abstract class CustomDropdownViewTemplate {
        public abstract UIElement GetView();
        public static implicit operator UIElement(CustomDropdownViewTemplate x) => x.GetView();
    }

    public class CustomDropdownView : CustomDropdownViewTemplate {
        private readonly UIElement _view;

        public CustomDropdownView(UIElement view) {
            _view = view;
        }

        public override UIElement GetView() {
            return _view;
        }
    }

    public class CustomDropdownItem : CustomDropdownViewTemplate {
        private readonly string _text;
        private readonly Action _callback;
        private readonly string _icon;

        private CustomFrame _view;

        public CustomDropdownItem(string text, Action callback = null, string icon = null) {
            _text = text;
            _callback = callback;
            _icon = icon;
        }

        public override UIElement GetView() {
            if (_view != null) return _view;

            var contentStack = new StackPanel {
                Padding = new Thickness(10),
                Orientation = Orientation.Horizontal,
                Background = Colors.Transparent.ToBrush()
            };

            var textLabel = new MDLabel {
                MaxLines = 1,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(_icon != null ? 8 : 0, 0, 0, 0),
                Text = _text
            };

            if (_icon != null) {
                var srcBmp = new BitmapImage {UriSource = new Uri(_icon)};
                var image = new Image {
                    Source = srcBmp,
                    Height = 24,
                    Width = 24
                };
                contentStack.Children.Add(image);
            }

            contentStack.Children.Add(textLabel);

            _view = new CustomFrame {
                Padding = new Thickness(6, 0, 6, 0),
                Margin = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                Background = Configuration.Theme.Background,
                Content = contentStack
            };

            _view.Touch += OnViewTouch;

            return _view;
        }

        private async void OnViewTouch(object sender, WTouchEventArgs args) {
            switch (args.ActionType) {
                // hover
                case WTouchAction.Entered:
                    await MainThread.InvokeOnMainThreadAsync(() => _view.Background = Configuration.Theme.ToolbarItemHover);
                    break;
                case WTouchAction.Exited:
                case WTouchAction.Cancelled:
                    await MainThread.InvokeOnMainThreadAsync(() => _view.Background = Configuration.Theme.Background);
                    break;

                // click
                case WTouchAction.Pressed:
                    await MainThread.InvokeOnMainThreadAsync(() => _view.Background = Configuration.Theme.ToolbarItemTap);

                    if (args.MouseButton == WMouseButton.Left) {
                        CustomDropdown.DropdownProtection = true;
                    }

                    break;
                case WTouchAction.Released:
                    await MainThread.InvokeOnMainThreadAsync(() => _view.Background = Configuration.Theme.ToolbarItemHover);

                    if (args.MouseButton == WMouseButton.Left) {
                        _callback?.Invoke();
                        CustomDropdown.CloseDropdown();
                    }

                    break;
            }
        }
    }

    public class CustomDropdownItemGroup : CustomDropdownViewTemplate {
        private readonly string _text;
        private readonly IEnumerable<CustomDropdownViewTemplate> _childItems;
        private readonly string _icon;

        private CustomFrame _view;
        private Frame _extraDropdown;
        private bool _extraDropdownHover;

        public CustomDropdownItemGroup(string text, IEnumerable<CustomDropdownViewTemplate> childItems, string icon = null) {
            _text = text;
            _childItems = childItems;
            _icon = icon;
        }

        public override UIElement GetView() {
            if (_view != null) return _view;

            var contentStack = new StackPanel {
                Padding = new Thickness(10),
                Orientation = Orientation.Horizontal,
                Background = Colors.Transparent.ToBrush()
            };

            var textLabel = new MDLabel {
                MaxLines = 1,
                TextAlignment = TextAlignment.Center,
                Text = _text
            };

            if (_icon != null) {
                var srcBmp = new BitmapImage {UriSource = new Uri(_icon)};
                var image = new Image {
                    Source = srcBmp,
                    Height = 24,
                    Width = 24
                };
                contentStack.Children.Add(image);
            }

            contentStack.Children.Add(textLabel);

            _view = new CustomFrame {
                Padding = new Thickness(6, 0, 6, 0),
                Margin = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                Background = Configuration.Theme.Background,
                Content = contentStack
            };

            _view.Touch += OnViewOnTouch;

            return _view;
        }

        private async void OnViewOnTouch(object sender, WTouchEventArgs args) {
            switch (args.ActionType) {
                // hover
                case WTouchAction.Entered:
                    await MainThread.InvokeOnMainThreadAsync(() => _view.Background = Configuration.Theme.ToolbarItemHover);

                    if (args.ActionType == WTouchAction.Pressed) CustomDropdown.DropdownProtection = true;

                    if (_extraDropdown != null) break;
                    var (selfX, selfY) = _view.GetAbsoluteCoordinates();
                    _extraDropdown = CustomDropdown.CreateDropdown(_childItems, ExtraTouchHandler);
                    const int paddingOffset = -7;

                    var minWidth = 120;

                    // check width
                    _extraDropdown.Width = Math.Max(minWidth, _extraDropdown.DesiredSize.Width);
                    if (selfX + _view.Width > Window.Current.Bounds.Width - 300) {
                        selfX -= 300;
                    } else {
                        selfX -= _extraDropdown.Width;
                    }

                    // check height
                    var height = _extraDropdown.DesiredSize.Height;
                    if (selfY + height > Window.Current.Bounds.Height) {
                        selfY -= selfY + height - Window.Current.Bounds.Height;
                    }

                    App.Page.AbsoluteOverlay.AddChild(_extraDropdown, new Point(selfX, selfY + paddingOffset));
                    CustomDropdown.DropdownClosed += OnDropdownClosed;
                    break;
                case WTouchAction.Exited:
                case WTouchAction.Cancelled:
                    if (_extraDropdown == null || _extraDropdownHover) break;

                    var relativeTouchPosition = args.Location;

                    var (viewX, viewY) = _view.GetAbsoluteCoordinates();
                    var absoluteTouchPosition = new Point(Math.Round(viewX + relativeTouchPosition.X), Math.Round(viewY + relativeTouchPosition.Y));
                    var touchMovedToExtraDropdown = _extraDropdown.HitTest(absoluteTouchPosition);

                    if (!touchMovedToExtraDropdown) {
                        await RemoveExtraDropdown();
                    }

                    break;
            }
        }

        private async void ExtraTouchHandler(object _, WTouchEventArgs extraArgs) {
            switch (extraArgs.ActionType) {
                case WTouchAction.Entered:
                case WTouchAction.Pressed:
                case WTouchAction.Released:
                case WTouchAction.Moved:
                    _extraDropdownHover = true;
                    break;
                case WTouchAction.Cancelled:
                case WTouchAction.Exited:
                    _extraDropdownHover = false;
                    await RemoveExtraDropdown();
                    break;
            }
        }

        private async Task RemoveExtraDropdown() {
            await MainThread.InvokeOnMainThreadAsync(() => {
                if (_view != null) {
                    _view.Background = Configuration.Theme.Background;
                }
            });

            if (_extraDropdown != null) {
                App.Page.AbsoluteOverlay.Children.Remove(_extraDropdown);
                ((CustomFrame) _extraDropdown).Touch -= ExtraTouchHandler;
                ((StackPanel) ((CustomFrame) _extraDropdown).Content)?.Children.Clear();
                _extraDropdown = null;
            }
        }

        private async void OnDropdownClosed() {
            CustomDropdown.DropdownClosed -= OnDropdownClosed;
            await RemoveExtraDropdown();

            if (_view != null) {
                _view.Touch -= OnViewOnTouch;
                _view = null;
            }
        }
    }
}