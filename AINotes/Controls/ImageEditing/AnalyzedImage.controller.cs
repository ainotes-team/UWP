using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AINotes.Components.Implementations;
using AINotes.Helpers.Imaging;
using AINotes.Models;
using Helpers;
using Helpers.Essentials;
using Helpers.Extensions;
using Imaging.Library;
using Imaging.Library.Entities;
using Imaging.Library.Enums;
using Imaging.Library.Filters.BasicFilters;
using Imaging.Library.Filters.ComplexFilters;
using Imaging.Library.Maths;
using Point = Windows.Foundation.Point;
using Size = Windows.Foundation.Size;

namespace AINotes.Controls.ImageEditing {
    public partial class AnalyzedImage {
                
        // image properties
        public byte[] DisplayBytes;
        public byte[] CroppedBytes;
        public byte[] AutoCroppedBytes;
        public readonly string AbsolutePath;
        private PixelMap _pixelMap;
        public Size ImageSize;
        public Size OriginalSize;

        // paper location
        public Point TopLeft;
        public Point TopRight;
        public Point BottomRight;
        public Point BottomLeft;
        
        public async Task<PixelMap> GetPixelMap() {
            Logger.Log("[AnalyzedImage]", "GetPixelMap");
            if (_pixelMap != null) return _pixelMap;
            
            var sources = new PixelMap[2];
            sources = await ImageEditingHelper.GetPixelMap(CroppedBytes, sources);
            _pixelMap = sources[0];

            return _pixelMap;
        }

        public async Task<ImageComponent> ToComponent() {
            Logger.Log("[AnalyzedImage]", "ToComponent");
            var model = new ComponentModel {
                Content = null,
                Type = "ImageComponent",
                FileId = App.EditorScreen.FileId,
                Deleted = false,
                ZIndex = -1,
                ComponentId = -1,
            };
                
            await model.SaveAsync();
                    
            // create component
            var imageComponent = new ImageComponent(model);
            var imagePath = imageComponent.GetImageSavingPath();
            
            await LocalFileHelper.WriteFileAsync(imagePath, DisplayBytes);
                
            imageComponent.SetContent(imagePath);
            
            LocalFileHelper.DeleteFile(AbsolutePath);
            LocalFileHelper.DeleteFile("auto_cropped.png");
            
            return imageComponent;
        }
        
        private EdgePoints PredictPaperPosition(PixelMap sourcePixelMap) {
            Logger.Log("[AnalyzedImage]", "PredictPaperPosition");
            const double scale = .5;
            
            var imaging = new ImagingManager(sourcePixelMap);
            
            var bicubic = new BicubicFilter(scale);
            imaging.AddFilter(bicubic);
            
            var canny = new CannyEdgeDetector();
            imaging.AddFilter(canny);
            
            imaging.Render();
            
            var blobCounter = new BlobCounter {
                ObjectsOrder = ObjectsOrder.Size,
            };
            imaging.AddFilter(blobCounter);
            
            imaging.Render();
            
            var blobs = blobCounter.GetObjectsInformation();
            List<Imaging.Library.Entities.Point> corners = null;
            foreach (var blob in blobs) {
                var points = blobCounter.GetBlobsEdgePoints(blob);
                if (points.Count < 2) continue;
            
                var shapeChecker = new SimpleShapeChecker();
                if (shapeChecker.IsQuadrilateral(points, out corners)) break;
            }
            
            
            // Undo every filters applied
            imaging.UndoAll();
            
            // actual resize
            var edgePoints = new EdgePoints();
            if (corners != null) edgePoints.SetPoints(corners.ToArray());
            // Corrects points that found on downscaled image to original
            edgePoints = edgePoints.ZoomIn(scale);

            return edgePoints;
        }

        public void Recalculate() {
            Logger.Log("[AnalyzedImage]", "Recalculate");
            AutoCrop(crop:false);
        }

        public void AutoCrop(bool setCropped = false, bool crop = true) {
            Logger.Log("[AnalyzedImage]", $"AutoCrop setCropped={setCropped}, crop={crop}");
            Task.Run(async () => {
                var sources = new PixelMap[2];
                sources = await ImageEditingHelper.GetPixelMap(CroppedBytes, sources);
                _pixelMap = sources[0];

                var pixelMap = await GetPixelMap();
            
                ImageSize.Width = pixelMap.Width;
                ImageSize.Height = pixelMap.Height;
                
                var predictedPaperEdgePoints = PredictPaperPosition(pixelMap);
            
                var topLeftX = predictedPaperEdgePoints.TopLeft.X;
                var topLeftY = predictedPaperEdgePoints.TopLeft.Y;
            
                var topRightX = predictedPaperEdgePoints.TopRight.X;
                var topRightY = predictedPaperEdgePoints.TopRight.Y;
            
                var bottomLeftX = predictedPaperEdgePoints.BottomLeft.X;
                var bottomLeftY = predictedPaperEdgePoints.BottomLeft.Y;
            
                var bottomRightX = predictedPaperEdgePoints.BottomRight.X;
                var bottomRightY = predictedPaperEdgePoints.BottomRight.Y;

                var predPoints = new List<Point> {
                    new Point(topLeftX, topLeftY),
                    new Point(topRightX, topRightY),
                    new Point(bottomLeftX, bottomLeftY),
                    new Point(bottomRightX, bottomRightY),
                    new Point(topLeftX, topLeftY),
                };
                
                // TODO: fix calc of ratio
                var predArea = Math.Abs(predPoints.Take(predPoints.Count - 1).Select((p, i) => 
                    p.X * predPoints[i + 1].Y - p.Y * predPoints[i + 1].X).Sum() / 2);

                var actualArea = ImageSize.Width * ImageSize.Height;
                
                Logger.Log($"[{nameof(AnalyzedImage)}]", $"{nameof(AutoCrop)} - Calculated {predArea} as predicted area & {actualArea} as actual image area");
                Logger.Log($"[{nameof(AnalyzedImage)}]", $"{nameof(AutoCrop)} - Ratio: {predArea / actualArea}");

                if (true) {
                    TopLeft = new Point(topLeftX,  topLeftY);
                    TopRight = new Point(topRightX,  topRightY);
                    BottomRight = new Point(bottomRightX,  bottomRightY);
                    BottomLeft = new Point(bottomLeftX,  bottomLeftY);
                    
                    if (crop) Transform(TopLeft, TopRight, BottomRight, BottomLeft, setCropped);
                } else {
                    TopLeft = new Point(0,  0);
                    TopRight = new Point(ImageSize.Width,  0);
                    BottomRight = new Point(ImageSize.Width,  ImageSize.Height);
                    BottomLeft = new Point(0,  ImageSize.Height);
                }
            });
        }

        public async void Transform(Point topLeft, Point topRight, Point bottomRight, Point bottomLeft, bool setCropped = false) {
            Logger.Log("[AnalyzedImage]", $"Transform setCropped={setCropped}");
            try {
                var (topLeftX, topLeftY) = topLeft;
                var (topRightX, topRightY) = topRight;
                var (bottomRightX, bottomRightY) = bottomRight;
                var (bottomLeftX, bottomLeftY) = bottomLeft;

                var finalEdgePoints = new EdgePoints();
                finalEdgePoints.SetPoints(new[] {
                    new Imaging.Library.Entities.Point(topLeftX, topLeftY),
                    new Imaging.Library.Entities.Point(topRightX, topRightY),
                    new Imaging.Library.Entities.Point(bottomLeftX, bottomLeftY),
                    new Imaging.Library.Entities.Point(bottomRightX, bottomRightY)
                });

                var imaging = new ImagingManager(await GetPixelMap());
                imaging.AddFilter(new QuadrilateralTransformation(finalEdgePoints, true));

                imaging.Render();

                await ImageEditingHelper.SavePixelMapToFile(imaging.Output);
                var resultStream = await ImageEditingHelper.SavePixelMapToStream(imaging.Output);

                var imgBytes = resultStream.ReadAllBytes();

                AutoCroppedBytes = imgBytes;

                if (!setCropped) return;
                CroppedBytes = imgBytes;
                DisplayBytes = imgBytes;

                await MainThread.InvokeOnMainThreadAsync(async () => { await SetImageSource(AutoCroppedBytes); });
            }
            catch (Exception) {
                // ignored
            }
        }
    }
}