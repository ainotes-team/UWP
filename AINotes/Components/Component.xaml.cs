using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using AINotes.Components.Implementations;
using AINotes.Controls.Pages;
using AINotes.Helpers;
using AINotes.Helpers.Geometry;
using AINotes.Helpers.Imaging;
using AINotes.Helpers.UserActions;
using AINotes.Models;
using Helpers;
using Helpers.Controls;
using MaterialComponents;
using Point = Windows.Foundation.Point;

namespace AINotes.Components {
    public abstract partial class Component {

        protected Component(ComponentModel componentModel) {
            InitializeComponent();

            // _model init
            if (componentModel == null) {
                _model = new ComponentModel();
            } else {
                _model = componentModel;
                ComponentId = componentModel.ComponentId;
                
                ModelChanged += OnModelChanged;

                LoadModel();
            }

            // content
            Border.BorderBrush = Configuration.Theme.ComponentBorder;
            Border.Background = Configuration.Theme.ComponentBackground;
            
            LoadContextMenuItems();

            Moved += OnMoved;
            Resized += OnResized;
            
            Deselected += OnDeselected;
            
            CreateUserAction = true;
        }

        protected virtual async void OnModelChanged(ComponentModel model) {
            await model.SaveAsync();
        }
        
        protected virtual void OnPositionChanged((double, double) position) {
            
        }
        
        protected virtual void OnSizeChanged((double, double) size) {
            
        }

        protected virtual void OnBoundsChanged(RectangleD bounds) {
            if (TouchStartBounds.X != -1 && TouchStartBounds.Y != -1 && TouchStartBounds.Width != -1 && TouchStartBounds.Height != -1) {
                if (GetX() == TouchStartBounds.X && GetY() == TouchStartBounds.Y && GetWidth() == TouchStartBounds.Width && GetHeight() == TouchStartBounds.Height) return;
                
                if (CreateUserAction) {
                    UserActionManager.OnComponentMoved(TouchStartBounds.Clone(), this);
                }
            }
        }
        
        protected virtual void OnZIndexChanged(int zIndex) {
            
        }
        
        protected virtual void OnDeletionChanged(bool deleted) {
            if (deleted) {
                Delete();
            } else {
                Add();
            }
        }
        
        protected virtual void OnContentChanged(string content) {
            
        }

        private void OnResized(RectangleD rectangle) {
            App.EditorScreen.InvokeComponentChanged(this);
        }

        private void OnMoved(RectangleD rectangle) {
            App.EditorScreen.InvokeComponentChanged(this);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs) {
            RepositionNobs();
        }

        private void LoadContextMenuItems() {
            // default actions
            ContextMenuActions = new Dictionary<string, Action> {
                {ResourceHelper.GetString("delete"), () => {
                        if (this is SelectionComponent selectionComponent) {
                            selectionComponent.Delete();
                        } else {
                            SetDeleted(true);
                        }
                    }
                },
                {ResourceHelper.GetString("copy"), Copy},
                {ResourceHelper.GetString("cut"), Cut}
            };
            
            // override methods for selection component => move all to foreground / background
            if (GetType() != typeof(SelectionComponent)) {
                ContextMenuActions.Add("To Foreground", () => {
                    var currentMaxZIndex = App.EditorScreen.GetDocumentComponents().Max(Canvas.GetZIndex);
                    Canvas.SetZIndex(this, currentMaxZIndex + 1);
                });
                
                ContextMenuActions.Add("To Background", () => {
                    var currentMinZIndex = App.EditorScreen.GetDocumentComponents().Min(Canvas.GetZIndex);
                    Canvas.SetZIndex(this, currentMinZIndex - 1);
                });
            }
        }

        private void OnDeselected(object sender, object eventArgs) {
            if (_model == null) return;
            App.EditorScreen.InvokeComponentChanged(this);
        }
        
        // loads moving / resizing nobs
        public void LoadNobs() {
            Logger.Log("[Component]", "LoadNobs()");
            if (Rotatable && ResizeableToRight) {
                throw new InvalidOperationException("A component can not be Rotatable & ResizeableToRight");
            }

            App.EditorScreen.RemoveAbsoluteOverlayElement(MovingNob);
            App.EditorScreen.RemoveAbsoluteOverlayElement(ResizingNob);
            App.EditorScreen.RemoveAbsoluteOverlayElement(ResizingToRightNob);

            void PreventFocusLossToAny(object sender, LosingFocusEventArgs args) {
                try {
                    args.Cancel = true;
                } catch (Exception ex) {
                    Logger.Log("Exception in PreventFocusLossToAny", ex, logLevel: LogLevel.Error);
                }
            }
            
            if (Movable) {
                App.EditorScreen.AddAbsoluteOverlayElement(MovingNob = new CustomFrame {
                    Background = Configuration.Theme.ComponentNobBackground,
                    Width = NobSize,
                    Height = NobSize,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
                    BorderBrush = Configuration.Theme.ComponentNobBorder,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(NobSize / 2.0),
                    Content = _movingNobImage = new Image {
                        Source = ImageSourceHelper.FromName(Icon.Move),
                        IsHitTestVisible = false,
                    },
                    CaptureTouch = true,
                });

                MovingNob.Touch += OnMovingNobTouch;
            
                // prevent refocus on release
                void OnMovingNobTouchPreventRefocus(object o, WTouchEventArgs args) {
                    var target = GetFocusTarget();
                    if (target == null) return;
                    switch (args.ActionType) {
                        case WTouchAction.Pressed:
                            target.LosingFocus += PreventFocusLossToAny;
                            break;
                        case WTouchAction.Cancelled:
                            target.LosingFocus -= PreventFocusLossToAny;
                            break;
                        case WTouchAction.Exited:
                            if (!args.InContact) target.LosingFocus -= PreventFocusLossToAny;
                            break;
                    }
                }

                MovingNob.Touch += OnMovingNobTouchPreventRefocus;
            
                // prevent scrolling on touch
                static void OnMovingNobTouchPreventScrolling(object o, WTouchEventArgs args) {
                    switch (args.ActionType) {
                        case WTouchAction.Pressed:
                            App.EditorScreen.ScrollDisabled = (true);
                            break;
                        case WTouchAction.Released:
                        case WTouchAction.Cancelled:
                        case WTouchAction.Exited:
                            App.EditorScreen.ScrollDisabled = (false);
                            break;
                    }
                }

                MovingNob.Touch += OnMovingNobTouchPreventScrolling;
            }
            
            if (Resizeable) {
                App.EditorScreen.AddAbsoluteOverlayElement(ResizingNob = new CustomFrame {
                    Background = Configuration.Theme.ComponentNobBackground,
                    Width = NobSize,
                    Height = NobSize,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
                    BorderBrush = Configuration.Theme.ComponentNobBorder,
                    CornerRadius = new CornerRadius(NobSize / 2.0),
                    BorderThickness = new Thickness(1),
                    Content = _resizingNobImage = new Image {
                        Source = ImageSourceHelper.FromName(Icon.Resize),
                        IsHitTestVisible = false,
                    },
                    CaptureTouch = true
                });
                
                ResizingNob.Touch += OnResizingNobTouch;
            
                // prevent refocus on release
                void OnResizingNobTouchPreventRefocus(object o, WTouchEventArgs args) {
                    var target = GetFocusTarget();
                    if (target == null) return;
                    switch (args.ActionType) {
                        case WTouchAction.Pressed:
                            target.LosingFocus += PreventFocusLossToAny;
                            break;
                        case WTouchAction.Cancelled:
                            target.LosingFocus -= PreventFocusLossToAny;
                            break;
                        case WTouchAction.Exited:
                            if (!args.InContact) target.LosingFocus -= PreventFocusLossToAny;
                            break;
                    }
                }

                ResizingNob.Touch += OnResizingNobTouchPreventRefocus;
            
                // prevent scrolling on touch
                static void OnResizingNobTouchPreventScrolling(object o, WTouchEventArgs args) {
                    switch (args.ActionType) {
                        case WTouchAction.Pressed:
                            CustomDropdown.CloseDropdown();
                            App.EditorScreen.ScrollDisabled = true;
                            break;
                        case WTouchAction.Released:
                            App.EditorScreen.ScrollDisabled = false;
                            break;
                        case WTouchAction.Cancelled:
                            break;
                        case WTouchAction.Exited:
                            break;
                    }
                }

                ResizingNob.Touch += OnResizingNobTouchPreventScrolling;
            }
            
            if (ResizeableToRight) {
                App.EditorScreen.AddAbsoluteOverlayElement(ResizingToRightNob = new CustomFrame {
                    Background = Configuration.Theme.ComponentNobBackground,
                    Width = NobSize,
                    Height = NobSize,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
                    BorderBrush = Configuration.Theme.ComponentNobBorder,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(NobSize / 2.0),
                    Content = _resizingToRightNobImage = new Image {
                        Source = ImageSourceHelper.FromName(Icon.ResizeHorizontal),
                        IsHitTestVisible = false,
                    },
                    CaptureTouch = true
                });
                
                ResizingToRightNob.Touch += OnResizingToRightNobTouch;

                // prevent refocus on release
                void OnResizingToRightTouchPreventRefocus(object o, WTouchEventArgs args) {
                    var target = GetFocusTarget();
                    if (target == null) return;
                    switch (args.ActionType) {
                        case WTouchAction.Pressed:
                            target.LosingFocus += PreventFocusLossToAny;
                            break;
                        case WTouchAction.Cancelled:
                            target.LosingFocus -= PreventFocusLossToAny;
                            break;
                        case WTouchAction.Exited:
                            if (!args.InContact) target.LosingFocus -= PreventFocusLossToAny;
                            break;
                    }
                }

                ResizingToRightNob.Touch += OnResizingToRightTouchPreventRefocus;
            
                // prevent scrolling on touch
                static void OnResizingToRightNobTouchPreventScrolling(object o, WTouchEventArgs args) {
                    switch (args.ActionType) {
                        case WTouchAction.Pressed:
                            CustomDropdown.CloseDropdown();
                            App.EditorScreen.ScrollDisabled = true;
                            break;
                        case WTouchAction.Released:
                        case WTouchAction.Cancelled:
                        case WTouchAction.Exited:
                            App.EditorScreen.ScrollDisabled = false;
                            break;
                    }
                }

                ResizingToRightNob.Touch += OnResizingToRightNobTouchPreventScrolling;
            }
            
            RepositionNobs();
        }
        
        protected void ResetTouchStartBounds() => TouchStartBounds = new RectangleD(-1, -1, -1, -1);
        private Point LastKnownOnNobPosition = new Point();
        
        private void OnResizingNobTouch(object o, WTouchEventArgs args) {
            if (!args.InContact && args.ActionType != WTouchAction.Released) {
                IsResizing = false;
                return;
            }

            Border.Visibility = HasBorder || (bool) Preferences.BorderDebugModeEnabled ? Visibility.Visible : Visibility.Collapsed;
            switch (args.ActionType) {
                case WTouchAction.Pressed:
                    CustomDropdown.CloseDropdown();
                    LastKnownOnNobPosition = TouchTrackingStart = new Point(args.Location.X, args.Location.Y);
                    TouchStartBounds = new RectangleD(_x, _y, GetWidth(), GetHeight());

                    IsResizing = true;
                    CreateUserAction = false;
                    App.EditorScreen.ScrollDisabled = true;
                    break;
                case WTouchAction.Moved:
                    if (IsResizing) {
                        var x = _x;
                        var y = _y;
                        var width = GetWidth();
                        var height = GetHeight();

                        // if proportional resizing
                        if (Shortcuts.PressedKeys.Contains(VirtualKey.Control)) {
                            var cursorSlope = -1;
                            var componentSlope = TouchStartBounds.Height / TouchStartBounds.Width;

                            var newXOnNob = args.Location.X;
                            var newYOnNob = args.Location.Y;

                            var startXOnNob = LastKnownOnNobPosition.X;
                            var startYOnNob = LastKnownOnNobPosition.Y;

                            var cursorStraight = new GeometryStraight(new GeometryPoint(newXOnNob, newYOnNob), cursorSlope);
                            var componentStraight = new GeometryStraight(new GeometryPoint(0, 0), componentSlope);

                            var intersection = Geometry.GetIntersection(cursorStraight, componentStraight);

                            if (width + intersection.X >= MinWidth && height + intersection.Y >= MinHeight) {
                                width += intersection.X;
                                height += intersection.Y;

                                LastKnownOnNobPosition = new Point(newXOnNob, newYOnNob);
                            }
                        } else {
                            if (GetWidth() - (TouchTrackingStart.X - args.Location.X) >= MinWidth) {
                                width = GetWidth() - (TouchTrackingStart.X - args.Location.X);
                            }

                            if (GetHeight() - (TouchTrackingStart.Y - args.Location.Y) >= MinHeight) {
                                height = GetHeight() - (TouchTrackingStart.Y - args.Location.Y);
                            }
                        }

                        App.EditorScreen.OnComponentNobMoving(new Point(_x + GetWidth() + args.Location.X, _y + GetHeight() + args.Location.Y));

                        var resized = x != _x || y != _y || width != GetWidth() || height != GetHeight();
                        if (resized) {
                            InvokeResizing(new RectangleD(x, y, width, height));
                            SetBounds(new RectangleD(x, y, width, height), true);
                        }
                    }

                    break;
                case WTouchAction.Cancelled:
                case WTouchAction.Released:
                    CreateUserAction = true;
                    
                    SetBounds(GetBounds());
                    Moved?.Invoke(GetBounds());
                    
                    ResetTouchStartBounds();
                    
                    App.EditorScreen.ScrollDisabled = false;
                    break;
            }
        }

        private void OnResizingToRightNobTouch(object o, WTouchEventArgs args) {
            if (!args.InContact && args.ActionType != WTouchAction.Released) {
                IsResizing = false;
                return;
            }

            Border.Visibility = HasBorder || (bool) Preferences.BorderDebugModeEnabled ? Visibility.Visible : Visibility.Collapsed;
            switch (args.ActionType) {
                case WTouchAction.Pressed:
                    CustomDropdown.CloseDropdown();
                    TouchTrackingStart = new Point(args.Location.X, args.Location.Y);

                    TouchStartBounds = new RectangleD(GetX(), GetY(), GetWidth(), GetHeight());

                    CreateUserAction = false;
                    IsResizing = true;
                    break;
                case WTouchAction.Moved:
                    if (IsResizing) {
                        var x = _x;
                        var y = _y;
                        var width = GetWidth();
                        var height = GetHeight();

                        if (GetWidth() - (TouchTrackingStart.X - args.Location.X) >= MinWidth) {
                            width = GetWidth() - (TouchTrackingStart.X - args.Location.X);
                        }

                        if (Shortcuts.PressedKeys.Contains(VirtualKey.Control)) {
                            height = (TouchStartBounds.Height / TouchStartBounds.Width) * width;
                        }

                        App.EditorScreen.OnComponentNobMoving(new Point(_x + GetWidth() + args.Location.X, _y + args.Location.Y));

                        InvokeResizing(new RectangleD(x, y, width, height));
                        SetBounds(new RectangleD(x, y, width, height), true);
                    }

                    break;
                case WTouchAction.Cancelled:
                case WTouchAction.Released:
                    CreateUserAction = true;
                    
                    SetBounds(GetBounds());
                    Moved?.Invoke(GetBounds());
                    
                    ResetTouchStartBounds();
                    
                    break;
            }
        }

        private void OnMovingNobTouch(object o, WTouchEventArgs args) {
            if (!args.InContact && args.ActionType != WTouchAction.Released) {
                IsMoving = false;
                return;
            }

            Border.Visibility = HasBorder || (bool) Preferences.BorderDebugModeEnabled ? Visibility.Visible : Visibility.Collapsed;
            switch (args.ActionType) {
                case WTouchAction.Pressed:
                    CustomDropdown.CloseDropdown();
                    TouchTrackingStart = new Point(args.Location.X, args.Location.Y);

                    TouchStartBounds = new RectangleD(GetX(), GetY(), GetWidth(), GetHeight());

                    CreateUserAction = false;
                    IsMoving = true;
                    break;
                case WTouchAction.Moved:
                    if (IsMoving) {
                        var x = GetX() + args.Location.X - TouchTrackingStart.X;
                        var y = GetY() + args.Location.Y - TouchTrackingStart.Y;
                        if (x < 0) x = 0;
                        if (y < 0) y = 0;
                        App.EditorScreen.OnComponentNobMoving(new Point(GetX() + args.Location.X, GetY() + args.Location.Y));
                        var moved = x != GetX() || y != GetY();
                        if (moved) {
                            Moving?.Invoke(new RectangleD(x, y, GetWidth(), GetHeight()));
                            SetBounds(new RectangleD(x, y, GetWidth(), GetHeight()), true);
                        }

                        RepositionNobs();
                    }

                    break;
                case WTouchAction.Cancelled:
                case WTouchAction.Released:
                    CreateUserAction = true;
                    
                    SetBounds(GetBounds());
                    Moved?.Invoke(GetBounds());
                    
                    ResetTouchStartBounds();
                    
                    break;
            }
        }


        public void RepositionNobs() {
            if (MovingNob != null) {
                Canvas.SetLeft(MovingNob, _x - NobSize / 2.0 - 2);
                Canvas.SetTop(MovingNob, _y - NobSize / 2.0 - 2);
            }
            
            if (ResizingNob != null) {
                Canvas.SetLeft(ResizingNob, _x + GetWidth() - NobSize / 2.0);
                Canvas.SetTop(ResizingNob, _y + GetHeight() - NobSize / 2.0);
            }
            
            if (ResizingToRightNob != null) {
                Canvas.SetLeft(ResizingToRightNob, _x + GetWidth() - NobSize / 2.0 + 1);
                Canvas.SetTop(ResizingToRightNob, _y - NobSize / 2.0 - 2);
            }

            UpdateDebugInfo();
        }

        public void RemoveNobs() {
            App.EditorScreen.RemoveAbsoluteOverlayElement(MovingNob);
            App.EditorScreen.RemoveAbsoluteOverlayElement(ResizingNob);
            App.EditorScreen.RemoveAbsoluteOverlayElement(ResizingToRightNob);
        }

        // select and focus the component
        public void Select() {
            Focus();
            IsSelected = true;
        }

        // deselect and unfocus the component
        public void Deselect() {
            Unfocus();
            IsSelected = false;
        }

        // add the component to editor
        public virtual void Add(bool undo = false) {
            Logger.Log("[Component]", $"-> {GetType()}.Add({undo})");
            CustomDropdown.CloseDropdown();
            
            Select();
            
            App.EditorScreen.AddContentComponent(this);

            if (undo) {
                UserActionManager.OnComponentAdded(this);
            }
        }

        // delete the component from the document
        public virtual void Delete() {
            Logger.Log("[Component]", $"Delete(undo: {CreateUserAction})");
            
            CustomDropdown.CloseDropdown();
            
            RemoveDebugInfo();
            RemoveNobs();
            App.EditorScreen.RemoveDocumentComponent(this);

            if (CreateUserAction) {
                UserActionManager.OnComponentDeleted(this);
            }
        }

        #region DebugInfo
        private StackPanel _debugInfoLayout;
        private MDLabel _debugInfoTypeLabel;
        private MDLabel _debugInfoComponentIdLabel;
        private MDLabel _debugInfoLiveIdLabel;
        private MDLabel _debugInfoRemoteIdLabel;
        private MDLabel _debugInfoPositionLabel;
        private MDLabel _debugInfoSizeLabel;
        
        private void UpdateDebugInfo() {
            if (!Preferences.DebugLabelsEnabled || GetType() == typeof(SelectionComponent)) return;
            Logger.Log("[Component]", "UpdateDebugInfo()");
            if (_debugInfoLayout == null) {
                _debugInfoTypeLabel = new MDLabel();
                _debugInfoComponentIdLabel = new MDLabel();
                _debugInfoLiveIdLabel = new MDLabel();
                _debugInfoRemoteIdLabel = new MDLabel();
                _debugInfoPositionLabel = new MDLabel();
                _debugInfoSizeLabel = new MDLabel();
                _debugInfoLayout = new StackPanel {
                    IsHitTestVisible = false,
                    Children = {
                        _debugInfoTypeLabel,
                        _debugInfoComponentIdLabel,
                        _debugInfoLiveIdLabel,
                        _debugInfoRemoteIdLabel,
                        _debugInfoPositionLabel,
                        _debugInfoSizeLabel,
                    }
                };
                App.EditorScreen.AddAbsoluteOverlayElement(_debugInfoLayout);
            }

            _debugInfoTypeLabel.Text = $"ComponentType: {GetType().Name}";
            _debugInfoComponentIdLabel.Text = $"ComponentId: {ComponentId}";
            _debugInfoLiveIdLabel.Text = $"Currently out of use";
            _debugInfoRemoteIdLabel.Text = $"RemoteId: {GetRemoteId()}";
            _debugInfoPositionLabel.Text = $"Position: {_x}|{_y}";
            _debugInfoSizeLabel.Text = $"Size: {Width}|{Height}";
            
            Canvas.SetLeft(_debugInfoLayout, _x + GetWidth() + 6);
            Canvas.SetTop(_debugInfoLayout, _y);
        }
        
        public void RemoveDebugInfo() {
            if (_debugInfoLayout == null) return;
            Logger.Log("[Component]", "RemoveDebugInfo()");
            App.EditorScreen.RemoveAbsoluteOverlayElement(_debugInfoLayout);
        }
        #endregion
    }
}