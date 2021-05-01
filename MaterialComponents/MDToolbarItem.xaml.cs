using System;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Helpers;

namespace MaterialComponents {
    public partial class MDToolbarItem {
        public ImageSource ImageSource {
            get => ToolbarItemImage.Source;
            set => ToolbarItemImage.Source = value;
        }

        public string AutomationName {
            set => AutomationProperties.SetName(this, value);
        }

        public bool Selectable { get; set; }
        public bool Deselectable { get; set; } = true; 

        // Color Settings
        public Brush OverrideColor = null;
        protected Brush DefaultColor => OverrideColor ?? Theming.CurrentTheme.TBIDefault;
        protected static Brush HoverColor => Theming.CurrentTheme.TBIHover;
        protected Brush TapColor => OverrideColor ?? Theming.CurrentTheme.TBITap;

        // States
        public bool IsHovering;
        
        private bool _isSelected;
        public bool IsSelected {
            get => _isSelected;
            set {
                _isSelected = value;
                Background = value ? TapColor : IsHovering ? HoverColor : DefaultColor;
                if (value) {
                    Selected?.Invoke();
                } else {
                    Deselected?.Invoke();
                }
            }
        }

        public new bool IsEnabled {
            get => base.IsEnabled;
            set {
                base.IsEnabled = value;
                Opacity = value ? 1.0 : 0.5;
            }
        }

        private bool _handleTouch;
        public bool HandleTouch {
            set {
                if (value == _handleTouch) return;
                if (value) {
                    PointerPressed += TouchHandler;
                    PointerReleased += TouchHandler;
                    DoubleTapped += TouchHandler;
                } else {
                    PointerPressed -= TouchHandler;
                    PointerReleased -= TouchHandler;
                    DoubleTapped -= TouchHandler;
                }
                _handleTouch = value;
            }
        }
        
        // Events
        public event Action Selected;
        public event Action Deselected;

        public event EventHandler<EventArgs> Pressed;
        public event EventHandler<EventArgs> RightPressed;
        public event EventHandler<EventArgs> PressedAgain;
        public event EventHandler<EventArgs> Released;

        public MDToolbarItem() {
            InitializeComponent();
            Background = DefaultColor;
            Touch += OnTouch;
        }
        
        public MDToolbarItem(string source, EventHandler<EventArgs> pressedEventHandler, bool selectable=false) : this() {
            if (source != null) ImageSource = new BitmapImage(new Uri(source)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache };
            if (pressedEventHandler != null) Pressed += pressedEventHandler;
            Selectable = selectable;
        }

        private void TouchHandler(object sender, DoubleTappedRoutedEventArgs e)  => e.Handled = true;
        private void TouchHandler(object sender, PointerRoutedEventArgs e) => e.Handled = true;
        
        private void OnTouch(object sender, WTouchEventArgs e) {
            switch (e.ActionType) {
                case WTouchAction.Entered:
                    if (!IsEnabled) break;
                    
                    OnHoverStart();
                    break;
                case WTouchAction.Pressed:
                    if (!IsEnabled) break;
                    
                    if (e.MouseButton == WMouseButton.Right) {
                        if (RightPressed != null) {
                            RightPressed.Invoke(this, EventArgs.Empty);
                        } else {
                            OnPressed();
                        }
                    } else {
                        OnPressed();
                    }
                    break;
                case WTouchAction.Released:
                    if (!IsEnabled) break;
                    
                    OnReleased();
                    break;
                case WTouchAction.Cancelled:
                case WTouchAction.Exited:
                    OnHoverEnd();
                    break;
                case WTouchAction.Moved:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void OnHoverStart() {
            if (Selectable) {
                Background = IsSelected ? TapColor : HoverColor;
            } else {
                Background = HoverColor;
            }

            IsHovering = true;
        }

        public void OnHoverEnd() {
            if (Selectable) {
                Background = IsSelected ? TapColor : DefaultColor;
            } else {
                Background = DefaultColor;
            }

            IsHovering = false;
        }

        public void SendPress() {
            OnPressed();
            OnReleased();
        }

        public void OnPressed() {
            if (Selectable) {
                if (IsSelected && !Deselectable) {
                    PressedAgain?.Invoke(this, EventArgs.Empty);
                    return;
                }

                IsSelected = !IsSelected;
                if (IsSelected) {
                    Selected?.Invoke();
                } else {
                    Deselected?.Invoke();
                }

                Background = IsSelected ? TapColor : IsHovering ? HoverColor : DefaultColor;
            } else {
                Background = TapColor;
            }

            Pressed?.Invoke(this, EventArgs.Empty);
        }

        public void OnReleased() {
            if (Selectable) {
                if (IsSelected && !Deselectable) return;
                Background = IsSelected ? TapColor : IsHovering ? HoverColor : DefaultColor;
            } else {
                Background = IsHovering ? HoverColor : DefaultColor;
            }

            Released?.Invoke(this, EventArgs.Empty);
        }
    }
}