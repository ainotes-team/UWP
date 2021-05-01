using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using AINotes.Controls;
using AINotes.Controls.ImageEditing;
using AINotes.Helpers.Imaging;
using Helpers;
using Helpers.Extensions;
using Imaging.Library;
using Imaging.Library.Entities;
using Imaging.Library.Filters.ComplexFilters;
using ILPoint = Imaging.Library.Entities.Point;

namespace AINotes.Screens {
    public partial class ImageEditorScreen {
        // state
        private List<AnalyzedImage> _analyzedImages;

        private byte[] _currentImageBytes;
        private string _currentImagePath;

        private RectangleD _imageBounds;
        private AnalyzedImage _currentAnalyzedImage;

        // state
        public Action<List<AnalyzedImage>> FinishCallback;
        
        public void LoadWithCallback(List<AnalyzedImage> images, Action<List<AnalyzedImage>> callback) {
            // set images
            _analyzedImages = images;
            
            // set callback
            FinishCallback = callback;
            
            // set image for editing
            SetCurrentImage(_analyzedImages[0]);
            
            // load
            App.Page.Load(this);
        }

        private void Finish() {
            FinishCallback?.Invoke(_analyzedImages);
        }
        
        private void SetCurrentImage(AnalyzedImage image) {
            if (_currentAnalyzedImage == image) return;
            Logger.Log("[ImageEditorScreen]", "SetCurrentImage");
            if (UIMode != ImageEditorMode.View) {
                UIMode = ImageEditorMode.View;
            }

            _currentImagePath = image.AbsolutePath;
            _currentAnalyzedImage = image;
            
            image.Select();
            
            SetCurrentImageBytes(image.DisplayBytes);
        }

        private async void SetCurrentImageBytes(byte[] imageBytes) {
            if (imageBytes == null) throw new ArgumentNullException(nameof(_currentImageBytes), @$"{nameof(_currentImageBytes)} cannot be null");
            Logger.Log("[ImageEditorScreen]", "SetCurrentImageBytes");
            _currentImageBytes = imageBytes;
            
            // update current image source
            using var stream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(stream.GetOutputStreamAt(0))) {
                writer.WriteBytes(_currentImageBytes);
                await writer.StoreAsync();
            }
            await _bitmap.SetSourceAsync(stream);
            EditingImage.Source = _bitmap;

            // calculate
            _currentAnalyzedImage.OnSizeCalculated += () => AdjustToContainerSize(_imageContainerSize.Width, _imageContainerSize.Height);
            _currentAnalyzedImage.OnSourceChanged();
            await _currentAnalyzedImage.SetImageSource(_currentImageBytes);
            
            // update previews
            UpdatePreviews();
        }

        private async void WriteCurrentImageFile(byte[] imageBytes) {
            if (imageBytes != null) await LocalFileHelper.WriteFileAsync(_currentImagePath, imageBytes);
        }

        private void DeleteCurrentImage() {
            var oldImage = _currentAnalyzedImage;

            if (_analyzedImages.Count >= 2) {
                var index = _analyzedImages.IndexOf(_currentAnalyzedImage);
                var newImage = index == 0 ? _analyzedImages[1] : _analyzedImages[index - 1];

                _currentImagePath = newImage.AbsolutePath;
                _currentAnalyzedImage = newImage;
            
                SetCurrentImage(newImage);
            } else {
                App.Page.Load(App.EditorScreen);
            }

            _analyzedImages.Remove(oldImage);
            PreviewsStack.Children.Remove(oldImage);
        }

        private async void Apply() {
            var topLeft = new ILPoint((_nobTopLeft.X + MovableCanvasNob.NobRadius - _imageBounds.X) / _imageBounds.Width * _currentAnalyzedImage.OriginalSize.Width, (_nobTopLeft.Y + MovableCanvasNob.NobRadius - _imageBounds.Y) / _imageBounds.Height * _currentAnalyzedImage.OriginalSize.Height);
            var topRight = new ILPoint((_nobTopRight.X + MovableCanvasNob.NobRadius - _imageBounds.X) / _imageBounds.Width * _currentAnalyzedImage.OriginalSize.Width, (_nobTopRight.Y + MovableCanvasNob.NobRadius - _imageBounds.Y) / _imageBounds.Height * _currentAnalyzedImage.OriginalSize.Height);
            var bottomLeft = new ILPoint((_nobBottomLeft.X + MovableCanvasNob.NobRadius - _imageBounds.X) / _imageBounds.Width * _currentAnalyzedImage.OriginalSize.Width, (_nobBottomLeft.Y + MovableCanvasNob.NobRadius - _imageBounds.Y) / _imageBounds.Height * _currentAnalyzedImage.OriginalSize.Height);
            var bottomRight = new ILPoint((_nobBottomRight.X + MovableCanvasNob.NobRadius - _imageBounds.X) / _imageBounds.Width * _currentAnalyzedImage.OriginalSize.Width, (_nobBottomRight.Y + MovableCanvasNob.NobRadius - _imageBounds.Y) / _imageBounds.Height * _currentAnalyzedImage.OriginalSize.Height);

            byte[] imageBytes;
            switch (UIMode) {
                case ImageEditorMode.Crop:
                    imageBytes = await GetCroppedBytes(_currentAnalyzedImage, topLeft, topRight, bottomLeft, bottomRight);
                    break;
                
                case ImageEditorMode.Transform:
                    imageBytes = await GetTransformedBytes(_currentAnalyzedImage, NobMoved, topLeft, topRight, bottomLeft, bottomRight);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            UIMode = ImageEditorMode.View;
            
            SetCurrentImageBytes(imageBytes);
            WriteCurrentImageFile(imageBytes);
        }

        private static void OpenCamera() {
            App.Page.Load(App.CameraScreen);
        }

        private static async Task<byte[]> GetCroppedBytes(AnalyzedImage image, ILPoint p1, ILPoint p2, ILPoint p3, ILPoint p4) {
            // create edge points
            var finalEdgePoints = new EdgePoints();
            finalEdgePoints.SetPoints(new[] {p1, p2, p3, p4});
            
            // apply transformation filter
            var imaging = new ImagingManager(await image.GetPixelMap());
            imaging.AddFilter(new QuadrilateralTransformation(finalEdgePoints, true));
            imaging.Render();

            // save result to stream
            await ImageEditingHelper.SavePixelMapToFile(imaging.Output);
            var resultStream = await ImageEditingHelper.SavePixelMapToStream(imaging.Output);
            var resultBytes = resultStream.ReadAllBytes();

            return resultBytes;
        }

        private static async Task<byte[]> GetTransformedBytes(AnalyzedImage image, bool nobMoved, ILPoint p1, ILPoint p2, ILPoint p3, ILPoint p4) {
            byte[] resultBytes;
            if (nobMoved || image.AutoCroppedBytes == null) {
                Logger.Log("[ImageEditorScreen]", "Controller: GetTransformedBytes - Recalculate");
                var finalEdgePoints = new EdgePoints();
                var points = new List<ILPoint> {p1, p2, p3, p4};
                points.Sort((point, point1) => point.Y.CompareTo(point1.Y));
                    
                var top = new List<ILPoint> {points[0], points[1]};
                top.Sort((point, point1) => point.X.CompareTo(point1.X));
                    
                var bottom = new List<ILPoint> {points[2], points[3]};
                bottom.Sort((point, point1) => point.X.CompareTo(point1.X));

                finalEdgePoints.SetPoints(new [] {top[0], top[1], bottom[0], bottom[1]});
                var imaging = new ImagingManager(await image.GetPixelMap());
                imaging.AddFilter(new QuadrilateralTransformation(finalEdgePoints, true));

                imaging.Render();

                await ImageEditingHelper.SavePixelMapToFile(imaging.Output);
                var resultStream = await ImageEditingHelper.SavePixelMapToStream(imaging.Output);

                resultBytes = resultStream.ReadAllBytes();
            } else {
                Logger.Log("[ImageEditorScreen]", "Controller: GetTransformedBytes - Pre-Cropped");
                resultBytes = image.UsePreCropped();
            }

            return resultBytes;
        }
    }
}