using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AINotes.Controls.Pages;
using AINotes.Helpers;
using Helpers;
using Helpers.Extensions;
using AINotes.Models;
using Helpers.Controls;
using Helpers.Essentials;
using Point = Windows.Foundation.Point;

namespace AINotes.Components {
    // TODO: Rotation
    public abstract partial class Component {

        #region ViewModel

        private readonly ComponentModel _model;

        public bool CreateUserAction;

        private event Action<ComponentModel> ModelChanged;

        // position & size
        private double _x;

        protected void SetX(double x) {
            _x = x;
            _model.PosX = x;
            
            OnPositionChanged((x, GetY()));
            ModelChanged?.Invoke(_model);
            
            Canvas.SetLeft(this, x);
        }

        public double GetX() => _x;
        
        
        private double _y;
        
        protected void SetY(double y) {
            _y = y;
            _model.PosY = y;
            
            OnPositionChanged((GetX(), y));
            ModelChanged?.Invoke(_model);
            
            Canvas.SetTop(this, y);
        }
        
        public double GetY() => _y;


        protected void SetWidth(double width) {
            Width = width;
            _model.SizeX = width;
            
            OnSizeChanged((width, GetHeight()));
            ModelChanged?.Invoke(_model);
        }
        
        public double GetWidth() => Width;
        
        
        protected void SetHeight(double height) {
            Height = height;
            _model.SizeY = height;
            
            OnSizeChanged((GetWidth(), height));
            ModelChanged?.Invoke(_model);
        }

        public double GetHeight() => Height;

        public void SetBounds(RectangleD bounds, bool doNotInvoke = false) {
            var (x, y, width, height) = bounds;
            _x = x;
            _model.PosX = x;
            
            Canvas.SetLeft(this, x);
            
            _y = y;
            _model.PosY = y;
            
            Canvas.SetTop(this, y);
            
            Width = width;
            _model.SizeX = width;
            
            Height = height;
            _model.SizeY = height;

            OnPositionChanged((bounds.X, bounds.Y));
            OnSizeChanged((bounds.Width, bounds.Height));
            
            OnBoundsChanged(bounds);

            if (!doNotInvoke) {
                ModelChanged?.Invoke(_model);
            }
        }

        public RectangleD GetBounds() => new RectangleD(_x, _y, Width, Height);
        
        
        private int _zIndex;

        public void SetZIndex(int zIndex) {
            _zIndex = zIndex;
            _model.ZIndex = zIndex;
            
            OnZIndexChanged(zIndex);
            ModelChanged?.Invoke(_model);
        }

        public int GetZIndex() => _zIndex;
        
        
        // deleted

        private bool _deleted;

        public void SetDeleted(bool deleted) {
            Logger.Log("[Component]", $"-> {GetType()}.SetDeleted({deleted})");
            _deleted = deleted;
            _model.Deleted = deleted;
            
            OnDeletionChanged(deleted);
            ModelChanged?.Invoke(_model);
        }

        public bool GetDeleted() => _deleted;
        
        
        // content

        private string _content;

        public void SetContent(string content) {
            if (content == null) {
                _content = null;
                return;
            }
            if (!content.Equals(_content)) {
                _content = content;
                _model.Content = content;
                
                OnContentChanged(content);
                ModelChanged?.Invoke(_model);
            }
        }

        public string GetContent() => _content;


        // ids

        private int _componentId;

        public int ComponentId {
            get => _componentId;
            set {
                if (_componentId == value) {
                    Logger.Log("[Component]", "Same ComponentId already assigned", value, logLevel: LogLevel.Warning);
                    return;
                }
                if (_componentId == default || _componentId == -1) {
                    _componentId = value;
                    if (_debugInfoComponentIdLabel != null) {
                        _debugInfoComponentIdLabel.Text = $"ComponentId: {_componentId}";
                    }
                } else {
                    throw new ArgumentOutOfRangeException(nameof(value), @"ComponentId already assigned");
                }
            }
        }
        
        private string _remoteId;

        public void SetRemoteId(string remoteId) {
            if (_remoteId == remoteId) {
                Logger.Log($"Same RemoteId already assigned {remoteId}", logLevel: LogLevel.Warning);
                return;
            }
            
            if (_remoteId == null) {
                Logger.Log("[Component]", "SetRemoteId:", remoteId);
                _remoteId = remoteId;
                _model.RemoteId = remoteId;
                
                ModelChanged?.Invoke(_model);
                
                if (_debugInfoRemoteIdLabel != null) {
                    _debugInfoRemoteIdLabel.Text = $"RemoteId: {remoteId}";
                }
            } else {
                Logger.Log(@$"RemoteId already assigned with id: {_remoteId}");
                //throw new ArgumentOutOfRangeException(nameof(remoteId), @$"RemoteId already assigned with id: {_remoteId}");
            }
        }

        public string GetRemoteId() => _remoteId;

        #endregion

        public ComponentModel GetModel() => _model;

        // nobs / border
        protected Point TouchTrackingStart;
        private const int NobSize = 25;

        protected CustomFrame MovingNob;
        private Image _movingNobImage;
        protected CustomFrame ResizingNob;
        private Image _resizingNobImage;
        protected CustomFrame ResizingToRightNob;
        private Image _resizingToRightNobImage;
        
        protected RectangleD TouchStartBounds;

        // settings
        protected new int MinHeight { get; set; } = 100;
        protected new int MinWidth { get; set; } = 100;
        protected bool Movable { get; set; } = true;
        protected bool Resizeable { get; set; } = true;
        protected bool ResizeableToRight { get; set; } = true;
        protected bool Rotatable { get; set; }

        private bool _hasBorder = true;
        protected bool HasBorder {
            get => _hasBorder;
            set {
                _hasBorder = value;
                Border.Visibility = _hasBorder ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        protected Dictionary<string, Action> ContextMenuActions;

        // states
        protected bool IsMoving;
        protected bool IsResizing;

        private bool _isSelected;
        public bool IsSelected {
            get => _isSelected;
            set {
                if (value != IsSelected) {
                    Logger.Log("[Component]", ComponentId, "=>", GetType().Name, value ? "selected" : "deselected");
                    if (value) {
                        App.EditorScreen?.SelectedContent.Add(this);
                        Selected?.Invoke(this, EventArgs.Empty);
                    } else {
                        App.EditorScreen?.SelectedContent.Remove(this);
                        Deselected?.Invoke(this, EventArgs.Empty);
                    }
                }
                _isSelected = value;
                Border.Visibility = _isSelected && HasBorder || (bool) Preferences.BorderDebugModeEnabled ? Visibility.Visible : Visibility.Collapsed;
                
                // do not show nobs if using lasso
                if (App.EditorScreen?.Selecting ?? false) return;

                if (MovingNob != null && _movingNobImage != null) {
                    MovingNob.Visibility = _movingNobImage.Visibility = _isSelected ? Visibility.Visible : Visibility.Collapsed;
                }

                if (ResizingNob != null && _resizingNobImage != null) {
                    ResizingNob.Visibility = _resizingNobImage.Visibility = _isSelected ? Visibility.Visible : Visibility.Collapsed;
                }

                if (ResizingToRightNob != null && _resizingToRightNobImage != null) {
                    ResizingToRightNob.Visibility = _resizingToRightNobImage.Visibility = _isSelected ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        public int LiveId { get; set; }

        // events
        public event EventHandler Selected;
        public event EventHandler Deselected;

        public event Action<RectangleD> Resizing;
        public event Action<RectangleD> Resized;
        public event Action<RectangleD> Moving;
        public event Action<RectangleD> Moved;
        
        protected void InvokeResizing(RectangleD rectangle) => Resizing?.Invoke(rectangle);
        protected void InvokeResized(RectangleD rectangle) => Resized?.Invoke(rectangle);
        
        protected void InvokeMoving(RectangleD rectangle) => Moving?.Invoke(rectangle);
        protected void InvokeMoved(RectangleD rectangle) => Moved?.Invoke(rectangle);


        #region Abstract methods
        protected abstract FrameworkElement GetFocusTarget();
        protected abstract void Focus();
        public abstract void Unfocus();
        #endregion


        protected async void LoadModel() {
            if (_model == null) return;
            if (_model.ComponentId == -1) {
                await _model.SaveAsync();
            }
            
            MainThread.BeginInvokeOnMainThread(() => {
                ComponentId = _model.ComponentId;
                _remoteId = _model.RemoteId;

                _x = _model.PosX;
                _y = _model.PosY;
                
                Canvas.SetLeft(this, _model.PosX);
                Canvas.SetTop(this, _model.PosY);

                Width = _model.SizeX;
                Height = _model.SizeY;

                _zIndex = _model.ZIndex;
                
                Canvas.SetZIndex(this, _model.ZIndex);

                _content = _model.Content;
                _deleted = _model.Deleted;

                CreateUserAction = false;

                OnPositionChanged((_x, _y));
                OnSizeChanged((Width, Height));
                OnZIndexChanged(_zIndex);
                OnContentChanged(_model.Content);

                OnModelChanged(_model);

                CreateUserAction = true;
            });
        }

        protected async Task DeleteFromDatabase() {
            if (_model == null) return;
            await FileHelper.DeleteComponentAsync(_model);
        }

        // open the components context menu
        protected void OpenContextMenu(Point p) {
            var items = new List<CustomDropdownViewTemplate>();
            foreach (var (key, value) in ContextMenuActions) {
                items.Add(new CustomDropdownItem(key, value));
            }
            
            if (!IsSelected) {
                Focus();
                if (!IsSelected) Select();
            }

            var (x, y) = p;
            CustomDropdown.ShowDropdown(items, new Point(x, y));
        }
        
        #region CopyCut
        public virtual void Copy() {
            CustomDropdown.CloseDropdown();
            ClipboardManager.TemporaryClipboard.Clear();
        }

        public virtual void Cut() {
            CustomDropdown.CloseDropdown();
            ClipboardManager.TemporaryClipboard.Clear();
            SetDeleted(true);
        }
        #endregion
    }
}