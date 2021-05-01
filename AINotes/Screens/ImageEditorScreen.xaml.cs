using System;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using AINotes.Controls;
using AINotes.Controls.ImageEditing;
using Helpers;
using Helpers.Controls;
using Helpers.Extensions;
using Point = Windows.Foundation.Point;
using Size = Windows.Foundation.Size;

namespace AINotes.Screens {
    public enum ImageEditorMode {
        View,
        Crop,
        Transform,
    }
    
    // TODO: Fix Preview Scrolling
    public partial class ImageEditorScreen {
        // nob state
        private bool NobMoved => _nobTopLeft.NobMoved || _nobBottomLeft.NobMoved || _nobTopRight.NobMoved || _nobBottomRight.NobMoved;

        // nobs
        private readonly MovableCanvasNob _nobTopLeft;
        private readonly MovableCanvasNob _nobBottomLeft;
        private readonly MovableCanvasNob _nobTopRight;
        private readonly MovableCanvasNob _nobBottomRight;

        // views
        private CustomFrame _cropOverlayRect = new CustomFrame();
        private readonly BitmapImage _bitmap = new BitmapImage();
        
        // ui state
        private Size _imageContainerSize = Size.Empty;

        private ImageEditorMode _uiMode;
        private ImageEditorMode UIMode {
            get => _uiMode;
            set {
                // set
                _uiMode = value;
                
                Logger.Log($"Set UIMode = {value}");
                
                // unload
                if (MainImageContainer.Children.Contains(_nobTopLeft)) {
                    MainImageContainer.Children.Remove(_nobTopLeft);
                    MainImageContainer.Children.Remove(_nobTopRight);
                    MainImageContainer.Children.Remove(_nobBottomLeft);
                    MainImageContainer.Children.Remove(_nobBottomRight);

                    if (MainImageContainer.Children.Contains(_cropOverlayRect)) MainImageContainer.Children.Remove(_cropOverlayRect);
                }

                // load
                switch (value) {
                    case ImageEditorMode.View:
                        ApplyButton.IsEnabled = CloseButton.IsEnabled = false;
                        EnterViewMode();
                        break;
                    case ImageEditorMode.Crop:
                        EnterCropMode();
                        ApplyButton.IsEnabled = CloseButton.IsEnabled = true;
                        break;
                    case ImageEditorMode.Transform:
                        EnterTransformMode();
                        ApplyButton.IsEnabled = CloseButton.IsEnabled = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public ImageEditorScreen() {
            InitializeComponent();
            
            // create nobs
            _nobTopLeft = new MovableCanvasNob { Background = new SolidColorBrush(Colors.Gray) };
            _nobBottomLeft = new MovableCanvasNob { Background = new SolidColorBrush(Colors.Gray) };
            _nobTopRight = new MovableCanvasNob { Background = new SolidColorBrush(Colors.Gray) };
            _nobBottomRight = new MovableCanvasNob { Background = new SolidColorBrush(Colors.Gray) };
            
            RegisterShortcuts();
        }

        public override void OnLoad() {
            Logger.Log("[ImageEditorScreen]", "OnLoad");
            base.OnLoad();

            // toolbar setup
            App.Page.OnBackPressed = () => App.Page.Load(App.EditorScreen);
            App.Page.PrimaryToolbarChildren.Clear();

            // reset to view mode
            UIMode = ImageEditorMode.View;
        }

        private void OnDeleteButtonClicked(object _, RoutedEventArgs routedEventArgs) {
            DeleteCurrentImage();
        }

        private void OnNewPicButtonClicked(object _, RoutedEventArgs routedEventArgs) {
            OpenCamera();
        }

        private void OnCornerAdjustButtonClicked(object _, RoutedEventArgs routedEventArgs) {
            UIMode = ImageEditorMode.Transform;
        }

        private void OnCropButtonClicked(object _, RoutedEventArgs routedEventArgs) {
            UIMode = ImageEditorMode.Crop;
        }
        
        private void OnSaveButtonClicked(object _, RoutedEventArgs routedEventArgs) {
            Finish();
        }
        
        private void OnApplyButtonClicked(object _, RoutedEventArgs routedEventArgs) {
            Apply();
        }
        
        private void OnCloseButtonClicked(object _, RoutedEventArgs routedEventArgs) {
            UIMode = ImageEditorMode.View;
        }

        private void OnMainImageContainerSizeChanged(object sender, SizeChangedEventArgs args) {
            AdjustToContainerSize(args.NewSize.Width, args.NewSize.Height);
        }

        private void OnPreviewClicked(object img, WTouchEventArgs args) {
            if (!args.InContact) return;
            if (args.ActionType != WTouchAction.Pressed) return;
            SetCurrentImage((AnalyzedImage) img);
            foreach (var analyzedImage in _analyzedImages) {
                analyzedImage.Deselect();
            }
            
            ((AnalyzedImage) img).Select();
        }
        
        private void UpdatePreviews() {
            PreviewsStack.Children.Clear();
            foreach (var img in _analyzedImages) {
                img.Touch += OnPreviewClicked;
                PreviewsStack.Children.Add(img);
            }
            PreviewsScrollView.Visibility = _analyzedImages.Count != 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AdjustToContainerSize(double newWidth, double newHeight) {
            // skip while initializing
            if (Math.Abs(newWidth) < 1 || Math.Abs(newHeight) < 1) return;
            
            // leave editing mode
            UIMode = ImageEditorMode.View;
            
            // update container size
            _imageContainerSize = new Size(newWidth, newHeight);
            newWidth -= Configuration.ImageEditorScreen.EditorPadding * 2;
            newHeight -= Configuration.ImageEditorScreen.EditorPadding * 2;
            
            // calculate new ratios
            var workspaceAspectRatio = newWidth * 1f / (newHeight * 1f);
            var imageAspectRatio = _currentAnalyzedImage.OriginalSize.Width * 1f / (_currentAnalyzedImage.OriginalSize.Height * 1f);
            
            // update the image container & image bounds accordingly
            if (workspaceAspectRatio >= imageAspectRatio) {
                EditingImage.Width = newHeight * imageAspectRatio;
                
                var innerX = (newWidth - newHeight * imageAspectRatio) / 2;
                
                Canvas.SetLeft(EditingImage, Configuration.ImageEditorScreen.EditorPadding + innerX);
                Canvas.SetTop(EditingImage, Configuration.ImageEditorScreen.EditorPadding);
                
                _imageBounds = new RectangleD(Configuration.ImageEditorScreen.EditorPadding + innerX, Configuration.ImageEditorScreen.EditorPadding, EditingImage.Width, newHeight);
            } else {
                EditingImage.Width = newWidth;
                
                var innerY = (newHeight - newWidth / imageAspectRatio) / 2;
                
                Canvas.SetLeft(EditingImage, Configuration.ImageEditorScreen.EditorPadding);
                Canvas.SetTop(EditingImage, Configuration.ImageEditorScreen.EditorPadding + innerY);
                
                _imageBounds = new RectangleD(Configuration.ImageEditorScreen.EditorPadding, Configuration.ImageEditorScreen.EditorPadding + innerY, EditingImage.Width, newWidth * 1f / imageAspectRatio);
            }
        }

        private void EnterViewMode([CallerMemberName] string callerMemberName=null) {
            if (callerMemberName != nameof(UIMode)) throw new InvalidProgramException(@$"{nameof(EnterViewMode)} max only be called by {UIMode}.");
            Logger.Log("[ImageEditorScreen]", "EnterViewMode");
            
            // TODO: Disable Buttons
        }

        private void EnterTransformMode([CallerMemberName] string callerMemberName=null) {
            if (callerMemberName != nameof(UIMode)) throw new InvalidProgramException(@$"{nameof(EnterTransformMode)} max only be called by {UIMode}.");
            Logger.Log("[ImageEditorScreen]", "EnterTransformMode");

            // calculate ratios & corners
            var xRatio = _imageBounds.Width / _currentAnalyzedImage.OriginalSize.Width;
            var yRatio = _imageBounds.Height / _currentAnalyzedImage.OriginalSize.Height;

            var xOffset = _imageBounds.X;
            var yOffset = _imageBounds.Y;

            var topLeftX = xOffset + _currentAnalyzedImage.TopLeft.X * xRatio;
            var topLeftY = yOffset + _currentAnalyzedImage.TopLeft.Y * yRatio;

            var topRightX = xOffset + _currentAnalyzedImage.TopRight.X * xRatio;
            var topRightY = yOffset + _currentAnalyzedImage.TopRight.Y * yRatio;

            var bottomLeftX = xOffset + _currentAnalyzedImage.BottomLeft.X * xRatio;
            var bottomLeftY = yOffset + _currentAnalyzedImage.BottomLeft.Y * yRatio;

            var bottomRightX = xOffset + _currentAnalyzedImage.BottomRight.X * xRatio;
            var bottomRightY = yOffset + _currentAnalyzedImage.BottomRight.Y * yRatio;

            // add nobs at the corners
            MainImageContainer.Children.Add(_nobTopLeft);
            Canvas.SetLeft(_nobTopLeft, topLeftX - MovableCanvasNob.NobRadius);
            Canvas.SetTop(_nobTopLeft, topLeftY - MovableCanvasNob.NobRadius);

            MainImageContainer.Children.Add(_nobTopRight);
            Canvas.SetLeft(_nobTopRight, topRightX - MovableCanvasNob.NobRadius);
            Canvas.SetTop(_nobTopRight, topRightY - MovableCanvasNob.NobRadius);

            MainImageContainer.Children.Add(_nobBottomLeft);
            Canvas.SetLeft(_nobBottomLeft, bottomLeftX - MovableCanvasNob.NobRadius);
            Canvas.SetTop(_nobBottomLeft, bottomLeftY - MovableCanvasNob.NobRadius);

            MainImageContainer.Children.Add(_nobBottomRight);
            Canvas.SetLeft(_nobBottomRight, bottomRightX - MovableCanvasNob.NobRadius);
            Canvas.SetTop(_nobBottomRight, bottomRightY - MovableCanvasNob.NobRadius);

            // nob functions
            bool TouchCondition() {
                return UIMode == ImageEditorMode.Transform;
            }

            Point PreprocessNewPosition(Point p) {
                var (x, y) = p;
                if (x < _imageBounds.X - MovableCanvasNob.NobRadius) x = _imageBounds.X - MovableCanvasNob.NobRadius;
                if (y < _imageBounds.Y - MovableCanvasNob.NobRadius) y = _imageBounds.Y - MovableCanvasNob.NobRadius;
                if (x > _imageBounds.X + _imageBounds.Width - MovableCanvasNob.NobRadius) x = _imageBounds.X + _imageBounds.Width - MovableCanvasNob.NobRadius;
                if (y > _imageBounds.Y + _imageBounds.Height - MovableCanvasNob.NobRadius) y = _imageBounds.Y + _imageBounds.Height - MovableCanvasNob.NobRadius;
                return new Point(x, y);
            }
            
            // set nob functions
            _nobTopLeft.TouchCondition = TouchCondition;
            _nobTopRight.TouchCondition = TouchCondition;
            _nobBottomLeft.TouchCondition = TouchCondition;
            _nobBottomRight.TouchCondition = TouchCondition;
            
            _nobTopLeft.PreprocessNewPosition = PreprocessNewPosition;
            _nobTopRight.PreprocessNewPosition = PreprocessNewPosition;
            _nobBottomLeft.PreprocessNewPosition = PreprocessNewPosition;
            _nobBottomRight.PreprocessNewPosition = PreprocessNewPosition;
        }

        private void EnterCropMode([CallerMemberName] string callerMemberName=null) {
            if (callerMemberName != nameof(UIMode)) throw new InvalidProgramException(@$"{nameof(EnterCropMode)} max only be called by {UIMode}.");
            Logger.Log("[ImageEditorScreen]", "EnterCropMode");
            
            // add overlay rect
            _cropOverlayRect = new CustomFrame {
                Background = Colors.Gray.ToBrush(),
                Opacity = .2,
                CaptureTouch = true
            };

            MainImageContainer.Children.Add(_cropOverlayRect);
            _cropOverlayRect.Width = _imageBounds.Width;
            _cropOverlayRect.Height = _imageBounds.Height;
            
            Canvas.SetLeft(_cropOverlayRect, _imageBounds.X);
            Canvas.SetTop(_cropOverlayRect, _imageBounds.Y);

            var overlayRectMoving = false;
            Point overlayRectTouchTrackingStart;
            _cropOverlayRect.Touch += (_, args) => {
                if (UIMode != ImageEditorMode.Crop) return;
                if (!args.InContact) {
                    overlayRectMoving = false;
                    return;
                }

                switch (args.ActionType) {
                    case WTouchAction.Pressed:
                        overlayRectTouchTrackingStart = new Point(args.Location.X, args.Location.Y);
                        overlayRectMoving = true;
                        break;
                    case WTouchAction.Moved:
                        if (overlayRectMoving) {
                            var x = _cropOverlayRect.X + args.Location.X - overlayRectTouchTrackingStart.X;
                            var y = _cropOverlayRect.Y + args.Location.Y - overlayRectTouchTrackingStart.Y;
                            if (x < _imageBounds.X) x = _imageBounds.X;
                            if (y < _imageBounds.Y) y = _imageBounds.Y;
                            if (x > _imageBounds.X + _imageBounds.Width - _cropOverlayRect.Width) x = _imageBounds.X + _imageBounds.Width - _cropOverlayRect.Width;
                            if (y > _imageBounds.Y + _imageBounds.Height - _cropOverlayRect.Height) y = _imageBounds.Y + _imageBounds.Height - _cropOverlayRect.Height;
                            
                            CanvasHelper.SetPosition(_nobTopLeft, new Point(x - MovableCanvasNob.NobRadius, y - MovableCanvasNob.NobRadius));
                            CanvasHelper.SetPosition(_nobTopRight, new Point(x + _cropOverlayRect.Width - MovableCanvasNob.NobRadius, y - MovableCanvasNob.NobRadius));
                            CanvasHelper.SetPosition(_nobBottomLeft, new Point(x - MovableCanvasNob.NobRadius, y + _cropOverlayRect.Height - MovableCanvasNob.NobRadius));
                            CanvasHelper.SetPosition(_nobBottomRight, new Point(x - MovableCanvasNob.NobRadius + _cropOverlayRect.Width, y - MovableCanvasNob.NobRadius + _cropOverlayRect.Height));

                            CanvasHelper.SetPosition(_cropOverlayRect, new Point(_nobTopLeft.X + MovableCanvasNob.NobRadius, _nobTopLeft.Y + MovableCanvasNob.NobRadius));
                            CanvasHelper.SetSize(_cropOverlayRect, new Size(_nobTopRight.X - _nobTopLeft.X, _nobBottomLeft.Y - _nobTopLeft.Y));
                        }

                        break;
                }
            };
            
            // add nobs at the corners
            MainImageContainer.Children.Add(_nobTopLeft);
            Canvas.SetLeft(_nobTopLeft, _imageBounds.X - MovableCanvasNob.NobRadius);
            Canvas.SetTop(_nobTopLeft, _imageBounds.Y - MovableCanvasNob.NobRadius);
            
            MainImageContainer.Children.Add(_nobTopRight);
            Canvas.SetLeft(_nobTopRight, _imageBounds.X + _imageBounds.Width - MovableCanvasNob.NobRadius);
            Canvas.SetTop(_nobTopRight, _imageBounds.Y - MovableCanvasNob.NobRadius);
            
            MainImageContainer.Children.Add(_nobBottomLeft);
            Canvas.SetLeft(_nobBottomLeft, _imageBounds.X - MovableCanvasNob.NobRadius);
            Canvas.SetTop(_nobBottomLeft, _imageBounds.Y + _imageBounds.Height - MovableCanvasNob.NobRadius);
            
            MainImageContainer.Children.Add(_nobBottomRight);
            Canvas.SetLeft(_nobBottomRight, _imageBounds.X + _imageBounds.Width - MovableCanvasNob.NobRadius);
            Canvas.SetTop(_nobBottomRight, _imageBounds.Y + _imageBounds.Height - MovableCanvasNob.NobRadius);

            // shared nob functions
            bool TouchCondition() {
                return UIMode == ImageEditorMode.Crop;
            }

            // set nob functions
            _nobTopLeft.TouchCondition = TouchCondition;
            _nobTopRight.TouchCondition = TouchCondition;
            _nobBottomLeft.TouchCondition = TouchCondition;
            _nobBottomRight.TouchCondition = TouchCondition;

            // shared preprocessing function
            Point SharedPreprocessing(Point p) {
                var (x, y) = p;
                if (x < _imageBounds.X - MovableCanvasNob.NobRadius) x = _imageBounds.X - MovableCanvasNob.NobRadius;
                if (y < _imageBounds.Y - MovableCanvasNob.NobRadius) y = _imageBounds.Y - MovableCanvasNob.NobRadius;
                if (x > _imageBounds.X + _imageBounds.Width - MovableCanvasNob.NobRadius) x = _imageBounds.X + _imageBounds.Width - MovableCanvasNob.NobRadius;
                if (y > _imageBounds.Y + _imageBounds.Height - MovableCanvasNob.NobRadius) y = _imageBounds.Y + _imageBounds.Height - MovableCanvasNob.NobRadius;
                return new Point(x, y);
            }
            
            // individual preprocessing functions
            _nobTopLeft.PreprocessNewPosition = p => {
                var (x, y) = SharedPreprocessing(p);
                if (x + MovableCanvasNob.NobRadius * 4 > _nobTopRight.X) x = _nobTopRight.X - MovableCanvasNob.NobRadius * 4;
                if (y + MovableCanvasNob.NobRadius * 4 > _nobBottomLeft.Y) y = _nobBottomLeft.Y - MovableCanvasNob.NobRadius * 4;

                CanvasHelper.SetPosition(_nobBottomLeft, new Point(x, _nobBottomLeft.Y));
                CanvasHelper.SetPosition(_nobTopRight, new Point(_nobTopRight.X, y));

                CanvasHelper.SetPosition(_cropOverlayRect, new Point(_nobTopLeft.X + MovableCanvasNob.NobRadius, _nobTopLeft.Y + MovableCanvasNob.NobRadius));
                CanvasHelper.SetSize(_cropOverlayRect, new Size(_nobTopRight.X - _nobTopLeft.X, _nobBottomLeft.Y - _nobTopLeft.Y));

                return new Point(x, y);
            };

            _nobTopRight.PreprocessNewPosition = p => {
                var (x, y) = SharedPreprocessing(p);
                if (x - MovableCanvasNob.NobRadius * 4 < _nobTopLeft.X) x = _nobTopLeft.X + MovableCanvasNob.NobRadius * 4;
                if (y + MovableCanvasNob.NobRadius * 4 > _nobBottomRight.Y) y = _nobBottomRight.Y - MovableCanvasNob.NobRadius * 4;

                CanvasHelper.SetPosition(_nobTopLeft, new Point(_nobTopLeft.X, y));
                CanvasHelper.SetPosition(_nobBottomRight, new Point(x, _nobBottomRight.Y));

                CanvasHelper.SetPosition(_cropOverlayRect, new Point(_nobTopLeft.X + MovableCanvasNob.NobRadius, _nobTopLeft.Y + MovableCanvasNob.NobRadius));
                CanvasHelper.SetSize(_cropOverlayRect, new Size(_nobTopRight.X - _nobTopLeft.X, _nobBottomLeft.Y - _nobTopLeft.Y));

                return new Point(x, y);
            };

            _nobBottomLeft.PreprocessNewPosition = p => {
                var (x, y) = SharedPreprocessing(p);
                if (x + MovableCanvasNob.NobRadius * 4 > _nobBottomRight.X) x = _nobBottomRight.X - MovableCanvasNob.NobRadius * 4;
                if (y - MovableCanvasNob.NobRadius * 4 < _nobTopLeft.Y) y = _nobTopLeft.Y + MovableCanvasNob.NobRadius * 4;

                CanvasHelper.SetPosition(_nobTopLeft, new Point(x, _nobTopLeft.Y));
                CanvasHelper.SetPosition(_nobBottomRight, new Point(_nobBottomRight.X, y));

                CanvasHelper.SetPosition(_cropOverlayRect, new Point(_nobTopLeft.X + MovableCanvasNob.NobRadius, _nobTopLeft.Y + MovableCanvasNob.NobRadius));
                CanvasHelper.SetSize(_cropOverlayRect, new Size(_nobTopRight.X - _nobTopLeft.X, _nobBottomLeft.Y - _nobTopLeft.Y));

                return new Point(x, y);
            };

            _nobBottomRight.PreprocessNewPosition = p => {
                var (x, y) = SharedPreprocessing(p);
                if (x - MovableCanvasNob.NobRadius * 4 < _nobBottomLeft.X) x = _nobBottomLeft.X + MovableCanvasNob.NobRadius * 4;
                if (y - MovableCanvasNob.NobRadius * 4 < _nobTopRight.Y) y = _nobTopRight.Y + MovableCanvasNob.NobRadius * 4;

                CanvasHelper.SetPosition(_nobTopRight, new Point(x, _nobTopRight.Y));
                CanvasHelper.SetPosition(_nobBottomLeft, new Point(_nobBottomLeft.X, y));

                CanvasHelper.SetPosition(_cropOverlayRect, new Point(_nobTopLeft.X + MovableCanvasNob.NobRadius, _nobTopLeft.Y + MovableCanvasNob.NobRadius));
                CanvasHelper.SetSize(_cropOverlayRect, new Size(_nobTopRight.X - _nobTopLeft.X, _nobBottomLeft.Y - _nobTopLeft.Y));

                return new Point(x, y);
            };
        }
    }
}