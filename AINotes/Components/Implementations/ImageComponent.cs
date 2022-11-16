using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using AINotes.Controls.ImageEditing;
using AINotes.Helpers;
using AINotes.Helpers.Extensions;
using AINotes.Helpers.Imaging;
using AINotes.Helpers.UserActions;
using Helpers;
using Helpers.Extensions;
using AINotes.Models;
using MaterialComponents;
using Clipboard = Windows.ApplicationModel.DataTransfer.Clipboard;
using Point = Windows.Foundation.Point;
using Size = Windows.Foundation.Size;

namespace AINotes.Components.Implementations {
    public class ImageComponent : Component {
        public readonly Image Content;

        public byte[] Data;
        public string Path;

        public Size OriginalImageSize = Size.Empty;

        public event Action Success;
        
        public ImageComponent(ComponentModel componentModel) : base(componentModel) {
            Movable = Resizeable = Rotatable = true;
            ResizeableToRight = false;

            Content = new Image {
                Stretch = Stretch.Fill,
            };

            Children.Add(Content);

            ContextMenuActions.Add("Edit", OpenImageEditor);
            ContextMenuActions.Add("Rotate Counterclockwise", () => Rotate(-90));
            ContextMenuActions.Add("Rotate Clockwise", () => Rotate(90));

            PointerPressed += OnPointerPressed;
            PointerReleased += OnPointerReleased;
            PointerMoved += OnPointerMoved;
            PointerExited += OnPointerExited;
            PointerCanceled += OnPointerCanceled;

            _focusDummy.LostFocus += OnFocusDummyLostFocus;
        }

        public static async Task<(ImageComponent, string)> New(int fileId) {
            var model = new ComponentModel {
                Content = null,
                Type = "ImageComponent",
                FileId = fileId,
                Deleted = false,
                ZIndex = -1,
                ComponentId = -1,
            };

            await model.SaveAsync();
                    
            // create component
            var imageComponent = new ImageComponent(model);
            var imagePath = imageComponent.GetImageSavingPath();
                    
            Logger.Log("Open Stream");
                    
            File.Create(LocalFileHelper.ToAbsolutePath(imagePath)).Dispose();

            return (imageComponent, imagePath);
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs args) {
            if (TouchHelper.GetMouseButton(args.GetCurrentPoint((UIElement) sender)) != WMouseButton.Left) return;

            IsMoving = true;
            args.Handled = true;

            TouchTrackingStart = args.GetCurrentPoint(this).Position;
            TouchStartBounds = new RectangleD(GetX(), GetY(), GetWidth(), GetHeight());

            foreach (var itm in App.EditorScreen.SelectedContent.ToList().Where(itm => itm != this)) {
                itm.Deselect();
            }

            CreateUserAction = false;

            Select();
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs args) {
            IsMoving = false;
            args.Handled = true;

            foreach (var itm in App.EditorScreen.SelectedContent.ToList().Where(itm => itm != this)) {
                itm.Deselect();
            }

            Select();

            var p = args.GetCurrentPoint((UIElement) sender);
            if (TouchHelper.GetMouseButton(p) == WMouseButton.Right) {
                var abs = this.GetAbsoluteCoordinates();
                OpenContextMenu(new Point(abs.X + p.Position.X, abs.Y + p.Position.Y));
            }

            CreateUserAction = true;

            SetBounds(GetBounds());
            ResetTouchStartBounds();
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs args) {
            if (TouchHelper.GetMouseButton(args.GetCurrentPoint((UIElement) sender)) != WMouseButton.Left) return;
            if (!IsMoving) return;
            if (!args.Pointer.IsInContact) {
                IsMoving = false;
                return;
            }

            var p = args.GetCurrentPoint((UIElement) sender);
            if (TouchHelper.GetMouseButton(p) != WMouseButton.Left) {
                IsMoving = false;
                return;
            }

            args.Handled = true;
            var x = GetX() + p.Position.X - TouchTrackingStart.X;
            var y = GetY() + p.Position.Y - TouchTrackingStart.Y;
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            var moved = x != GetX() || y != GetY();
            if (moved) {
                SetBounds(new RectangleD(x, y, GetWidth(), GetHeight()), true);
            }

            RepositionNobs();
        }

        private void OnPointerExited(object s, PointerRoutedEventArgs e) {
            IsMoving = false;
        }

        private void OnPointerCanceled(object s, PointerRoutedEventArgs e) {
            IsMoving = false;
        }

        private void OnFocusDummyLostFocus(object s, RoutedEventArgs e) {
            Deselect();
        }

        public async void SetImageData(byte[] imageData, bool changeTimestamp = false) {
            Resizeable = Movable = true;
            Data = imageData;
            
            async Task SetImageSource(byte[] imageBytes) {
                Data = imageBytes ?? throw new ArgumentNullException(nameof(imageBytes), @"imageBytes cannot be null");
                using var stream = new InMemoryRandomAccessStream();
                using (var writer = new DataWriter(stream.GetOutputStreamAt(0))) {
                    writer.WriteBytes(imageBytes);
                    await writer.StoreAsync();
                }
                _bitmap ??= new BitmapImage {
                    CreateOptions = BitmapCreateOptions.IgnoreImageCache,
                };
                await _bitmap.SetSourceAsync(stream);
                OriginalImageSize = new Size(_bitmap.PixelWidth, _bitmap.PixelHeight);
                Content.Source = _bitmap;
            
                Logger.Log("Set new Image Data with original Size:", OriginalImageSize.ToString());
            
                if (!Succeeded) Success?.Invoke();
                Succeeded = true;
                LoadNobs();
                IsSelected = IsSelected;
            }
            
            await SetImageSource(imageData);
            if (changeTimestamp) {
                GetModel().ContentLastChanged = Time.CurrentTimeMillis();
                await GetModel().SaveAsync();
            }
        }

        private BitmapImage _bitmap;

        public void OnEditorScreenUnload() {
            if (App.EditorScreen.GetDocumentComponents().Contains(this)) return;
            LocalFileHelper.DeleteFile(Path);
        }

        public bool Succeeded { get; set; }

        private async void Rotate(int degrees) {
            SetImageData(await ImageEditingHelper.Rotate(Data, degrees), true);
            while (degrees < 0) degrees += 360;
            var h = GetHeight();
            var w = GetWidth();
            switch (degrees) {
                case 0:
                case 180:
                    break;
                case 90:
                case 270:
                    SetBounds(new RectangleD(GetX(), GetY(), h, w));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(degrees), @"currently only multiples of 90° are supported");
            }

            ShouldWriteToFile = true;
        }
        
        private void OpenImageEditor() {
            Logger.Log("[ImageComponent]", "OpenImageEditor");
            
            App.ImageEditorScreen.LoadWithCallback(new List<AnalyzedImage> {
                new AnalyzedImage(Data, Path ?? GetImageSavingPath()),
            }, editedImages => {
                Logger.Log("[ImageComponent]", "OpenImageEditor: FinishCallback");
                OnSourceChanged(Data, editedImages[0].DisplayBytes);
                SetImageData(editedImages[0].DisplayBytes, true);
                
                App.Page.Load(App.EditorScreen);
            });
        }

        private void OnSourceChanged(byte[] oldSource, byte[] newSource) {
            UserActionManager.AddUserAction(new UserAction(objects => {
                SetImageData((byte[]) objects["oldSource"], true);
            }, objects => {
                SetImageData((byte[]) objects["newSource"], true);
            },
            new Dictionary<string, object> {
                {"oldSource", oldSource},
                {"newSource", newSource}
            }));
        }

        public bool ShouldWriteToFile { get; set; }
        
        private readonly TextBox _focusDummy = new TextBox {
            IsReadOnly = true,
            IsHitTestVisible = false
        };
        private bool _focusDummyAdded;

        protected override void Focus() {
            if (!_focusDummyAdded) {
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

        public override void Unfocus() {
            // Deselect();
        }

        public static string GetImageSavingPathStatic(int componentId) {
            Logger.Log("-> GetImageSavingPathStatic", componentId);
            var path = $"component_data/image/{componentId}.png";
            return path;
        }
        
        public string GetImageSavingPath() {
            Logger.Log("-> GetImageSavingPath", ComponentId, "|", Path);
            if (Path != null) return Path;

            var path = $"component_data/image/{ComponentId}.png";
            Logger.Log("<- GetImageSavingPath:", path);
            return path;
        }

        private const string PasteFileName = "paste.png";

        public override async void Copy() {
            base.Copy();
            var dp = new DataPackage {
                RequestedOperation = DataPackageOperation.Copy
            };
            
            await LocalFileHelper.WriteFileAsync(PasteFileName, Data);
            
            var file = await StorageFile.GetFileFromPathAsync(LocalFileHelper.ToAbsolutePath(PasteFileName));
            dp.SetBitmap(RandomAccessStreamReference.CreateFromFile(file));
            Clipboard.SetContent(dp);
        }

        public override async void Cut() {
            base.Cut();
            var dp = new DataPackage {
                RequestedOperation = DataPackageOperation.Copy
            };
            
            await LocalFileHelper.WriteFileAsync(PasteFileName, Data);
            
            var file = await StorageFile.GetFileFromPathAsync(LocalFileHelper.ToAbsolutePath(PasteFileName));
            dp.SetBitmap(RandomAccessStreamReference.CreateFromFile(file));
            Clipboard.SetContent(dp);
        }
        
        public static async Task<ImageComponent> CreateImageComponent(string content, RectangleD bounds) {
            var model = new ComponentModel {
                ComponentId = -1,
                Type = "ImageComponent",
                Content = null,
                Deleted = false,
                ZIndex = -1,
                PosX = bounds.X,
                PosY = bounds.Y,
                SizeX = bounds.Width,
                SizeY = bounds.Height
            };

            await model.SaveAsync();
            
            return new ImageComponent(model);
        }
        
        protected override async void OnModelChanged(ComponentModel model) {
            base.OnModelChanged(model);

            if (model == null) return;
            if (string.IsNullOrEmpty(model.Content) || model.Content == Path) return;
            Path = model.Content;
            try {
                var newContent = await LocalFileHelper.ReadBytes(model.Content);
                SetImageData(newContent);
            } catch (IOException ex) {
                Logger.Log("[ImageComponent]", $"OnModelChanged: ReadBytes failed ({model.Content}):", ex, logLevel: LogLevel.Error);
                App.Page.Load(App.FileManagerScreen);
                App.Page.Notifications.Add(new MDNotification($"Error:\nCannot access {model.Content}.\nIt is currently locked by another process."));
                SentryHelper.CaptureCaughtException(ex);
            }
        }
    }
}