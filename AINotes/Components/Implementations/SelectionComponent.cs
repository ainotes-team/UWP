using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AINotes.Controls;
using AINotes.Controls.Pages;
using AINotes.Helpers;
using AINotes.Helpers.Extensions;
using AINotes.Helpers.UserActions;
using Helpers;
using Helpers.Extensions;
using Helpers.Controls;
using Point = Windows.Foundation.Point;

namespace AINotes.Components.Implementations {
    public class SelectionComponent : Component {
        private CustomInkCanvas _canvas;
        private List<Component> _children;
        private readonly CustomFrame _touchContent = new CustomFrame {
            Background = Colors.Black.ToBrush(0.1),
            Padding = new Thickness(0),
            Margin = new Thickness(0),
            CornerRadius = new CornerRadius(0),
            CaptureTouch = true,
        };
        private bool _touchHandlerSet;

        public SelectionComponent() : base(null) {
            CreateUserAction = false;
            
            Movable = Resizeable = true;
            MinWidth = MinHeight = 10;
            ResizeableToRight = false;
            HasBorder = false;
            IsSelected = true;
            Padding = Margin = new Thickness(0);
            
            Moving += d => OnMoving(d);
            Resizing += d => OnResizing(d);

            Children.Add(_touchContent);

            LoadNobs();
        }

        protected override void OnBoundsChanged(RectangleD bounds) {
            if (!CreateUserAction) return;

            var from = TouchStartBounds.Clone();
            var to = GetBounds();
            
            var inkStrokeIds = App.EditorScreen.GetSelectedInkStrokeIds()?.ToArray() ?? new int[0];
            var components = _children?.ToArray() ?? new Component[0];
            
            if (inkStrokeIds.Length == 0 && components.Length == 0) return;
            
            UserActionManager.OnComponentsStrokesBoundsChanged(from, to, components, inkStrokeIds);
                
            foreach (var component in components) {
                component.CreateUserAction = false;
                component.SetBounds(component.GetBounds());
                component.CreateUserAction = true;
            }

            CreateUserAction = true;
        }

        public void OnMoving(RectangleD nRect, List<int> strokeIds = null) {
            var (x, y, _, _) = nRect;
            var xOffset = x - GetX();
            var yOffset = y - GetY();

            _canvas.OnSelectionComponentMoving(GetBounds(), nRect, strokeIds);

            foreach (var c in _children) {
                c.CreateUserAction = false;
                c.SetBounds(new RectangleD(c.GetX() + xOffset, c.GetY() + yOffset, c.GetWidth(), c.GetHeight()), true);
                c.CreateUserAction = true;
                c.RepositionNobs();
                App.EditorScreen.InvokeComponentChanged(c);
            }

            if (!_focusDummyHasFocus) {
                Focus();
            }
        }

        public void OnResizing(RectangleD nRect, List<int> strokeIds = null) {
            var (_, _, width, height) = nRect;
            var xFactor = width / GetWidth();
            var yFactor = height / GetHeight();

            _canvas.OnSelectionComponentResizing(GetBounds(), nRect, strokeIds);
            foreach (var c in _children) {
                c.CreateUserAction = false;
                c.SetBounds(new RectangleD((c.GetX() - GetX()) * xFactor + GetX(), (c.GetY() - GetY()) * yFactor + GetY(), c.Width * xFactor, c.Height * yFactor), true);
                c.CreateUserAction = true;
                c.RepositionNobs();
                App.EditorScreen.InvokeComponentChanged(c);
            }

            if (!_focusDummyHasFocus) {
                Focus();
            }
        }

        private readonly TextBox _focusDummy = new TextBox {
            IsReadOnly = true,
            IsHitTestVisible = false
        };

        private bool _focusDummyAdded;
        private bool _focusDummyHasFocus;

        protected override void Focus() {
            if (!_focusDummyAdded) {
                _focusDummy.LostFocus += (_, _) => _focusDummyHasFocus = false;
                _focusDummy.GotFocus += (_, _) => _focusDummyHasFocus = true;
                _focusDummyAdded = true;
                App.Page.AbsoluteOverlay.AddChild(_focusDummy, new RectangleD(0, 0, 0, 0));
            }

            _focusDummy.Focus(FocusState.Pointer);
        }

        protected override FrameworkElement GetFocusTarget() {
            // ReSharper disable once InvertIf
            if (!_focusDummyAdded) {
                _focusDummyAdded = true;
                App.Page.AbsoluteOverlay.AddChild(_focusDummy, new RectangleD(0, 0, 0, 0));
            }

            return _focusDummy;
        }

        public override void Unfocus() { }

        public void SetTouchHandler() {
            if (_touchHandlerSet) return;

            _touchContent.Touch += OnTouchContentTouch;

            // prevent scrolling on touch
            _touchContent.Touch += OnTouchContentTouchPreventScrolling;

            _touchHandlerSet = true;

            if (!_focusDummyHasFocus) {
                Focus();
            }
        }

        private void OnTouchContentTouch(object sender, WTouchEventArgs args) {
            if (args.ActionType == WTouchAction.Released) {
                CustomDropdown.CloseDropdown();
                // open context menu on right click
                if (args.MouseButton == WMouseButton.Right) {
                    var (absoluteX, absoluteY) = _touchContent.GetAbsoluteCoordinates();
                    OpenContextMenu(new Point(absoluteX + args.Location.X * App.EditorScreen.ScrollZoom, absoluteY + args.Location.Y * App.EditorScreen.ScrollZoom));
                    return;
                }

                CreateUserAction = true;

                SetBounds(GetBounds());
                
                foreach (var component in _children) {
                    component.CreateUserAction = false;
                    component.SetBounds(component.GetBounds());
                    component.CreateUserAction = true;
                }
                
                IsMoving = false;
                return;
            }

            if (!args.InContact) return;

            switch (args.ActionType) {
                case WTouchAction.Entered:
                    break;
                case WTouchAction.Pressed:
                    CustomDropdown.CloseDropdown();
                    TouchTrackingStart = new Point(args.Location.X, args.Location.Y);

                    TouchStartBounds = GetBounds();
                    CreateUserAction = false;
                    
                    foreach (var component in _children) {
                        component.CreateUserAction = false;
                    }

                    IsMoving = true;

                    // open context menu on long press
                    if (args.Id == -10) OpenContextMenu(new Point(args.Location.X * App.EditorScreen.ScrollZoom, args.Location.Y * App.EditorScreen.ScrollZoom));
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
                            InvokeMoving(new RectangleD(x, y, GetWidth(), GetHeight()));
                            SetBounds(new RectangleD(x, y, GetWidth(), GetHeight()));
                            InvokeMoved(new RectangleD(x, y, GetWidth(), GetHeight()));
                        }

                        RepositionNobs();
                    }

                    break;
                case WTouchAction.Released:
                case WTouchAction.Cancelled:
                case WTouchAction.Exited:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnTouchContentTouchPreventScrolling(object o, WTouchEventArgs args) {
            switch (args.ActionType) {
                case WTouchAction.Pressed:
                    App.EditorScreen.ScrollDisabled = true;
                    break;
                case WTouchAction.Released:
                case WTouchAction.Cancelled:
                case WTouchAction.Exited:
                    App.EditorScreen.ScrollDisabled = false;
                    break;
            }
        }

        public override void Delete() {
            CustomDropdown.CloseDropdown();
            
            var strokeDeletionUserAction = _canvas.DeleteAllSelectedStrokes();
            
            _canvas.ClearSelection();
            App.EditorScreen.EndSelection();

            foreach (var c in _children) {
                c.CreateUserAction = false;
                
                c.SetDeleted(true);
                c.RemoveDebugInfo();

                c.CreateUserAction = true;
            }

            if (CreateUserAction) {
                UserActionManager.OnComponentsStrokesDeleted(_children.ToArray(), strokeDeletionUserAction);
            }

            RemoveNobs();
            _children.Clear();

            if (!_focusDummyHasFocus) {
                Focus();
            }
        }

        public void SetInkCanvas(CustomInkCanvas canvas) => _canvas = canvas;

        public void SetChildren(IEnumerable<Component> children) {
            _children = children.Where(itm => !(itm is SelectionComponent)).ToList();
        }

        public override void Copy() {
            CustomDropdown.CloseDropdown();
            ClipboardManager.TemporaryClipboard.Clear();

            ClipboardManager.SelectionPosition = GetBounds().Position;

            // when only one component is selected
            if (_children.Count >= 1) {
                var components = App.EditorScreen.GetDocumentComponents().Where(element => element.IsSelected);

                foreach (var component in components) {
                    var model = component.GetModel();
                    
                    ClipboardManager.TemporaryClipboard.Add(model);
                }
            }

            _canvas.CopyStrokes(GetBounds().Position);

            _canvas.ClearSelection();
            App.EditorScreen.EndSelection();

            _children.Clear();
        }

        public override void Cut() {
            CustomDropdown.CloseDropdown();
            ClipboardManager.TemporaryClipboard.Clear();

            ClipboardManager.SelectionPosition = GetBounds().Position;

            // when only one component is selected
            if (_children.Count >= 1) {
                var components = App.EditorScreen.GetDocumentComponents().Where(element => element.IsSelected);

                foreach (var component in components) {
                    var model = component.GetModel();

                    component.CreateUserAction = false;
                    component.SetDeleted(true);
                    component.CreateUserAction = true;
                    
                    ClipboardManager.TemporaryClipboard.Add(model);
                }
            }

            var userAction = _canvas.CutStrokes();

            _canvas.ClearSelection();
            App.EditorScreen.EndSelection();

            UserActionManager.OnComponentsStrokesDeleted(_children.ToArray(), userAction);

            _children.Clear();
        }
    }
}