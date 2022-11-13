using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Helpers;
using Helpers.Essentials;
using Helpers.Extensions;

namespace AINotes.Helpers.Geometry {
    public class HoughTransform {
        private const int NeighbourhoodSize = 4;

        private const int MaxTheta = 180;

        private const double ThetaStep = Math.PI / MaxTheta;

        protected readonly int Width, Height;

        protected int[,] HoughArray;

        protected float CenterX, CenterY;

        protected int HoughHeight;

        protected int DoubleHeight;

        protected int NumPoints;

        private double[] _sinCache;
        private double[] _cosCache;
        private static GeometryPolyline _polyline;

        public HoughTransform(GeometryPolyline polyline) {
            Width = (int) polyline.Bounds.Width;
            Height = (int) polyline.Bounds.Height;
            
            Initialise();
            AddPoints(polyline);
        }

        public void Initialise() {
            HoughHeight = (int) (Math.Sqrt(2) * Math.Max(Height, Width)) / 2;

            DoubleHeight = 2 * HoughHeight;

            HoughArray = new int[MaxTheta, DoubleHeight];

            CenterX = Width / 2f;
            CenterY = Height / 2f;

            NumPoints = 0;

            _sinCache = new double[MaxTheta];
            _cosCache = _sinCache.ToArray();
            for (var t = 0; t < MaxTheta; t++) {
                var realTheta = t * ThetaStep;
                _sinCache[t] = Math.Sin(realTheta);
                _cosCache[t] = Math.Cos(realTheta);
            }
        }

        public void AddPoints(GeometryPolyline polyline) {
            _polyline = polyline;
            
            Logger.Log("total Count: ", polyline.Points.Count);
            foreach (var point in polyline.Points) {
                AddPoint((int) (point.X - polyline.Bounds.X), (int) (point.Y - polyline.Bounds.Y));
            }
        }

        public void AddPoint(int x, int y) {
            for (var t = 0; t < MaxTheta; t+=1) {
                var r = (int) ((x - CenterX) * _cosCache[t] + (y - CenterY) * _sinCache[t]);

                r += HoughHeight;

                if (r < 0 || r >= DoubleHeight) continue;

                HoughArray[t, r]++;
            }

            NumPoints++;
        }
        
        public List<HoughLine> GetLines() {
            var threshold = _polyline.Bounds.GetShortSide() / 14;

            var unfilteredLines = new List<HoughLine>(30);

            if (NumPoints == 0) return unfilteredLines;

            bool br;
            
            for (var t = 0; t < MaxTheta; t++) {
                for (var r = NeighbourhoodSize; r < DoubleHeight - NeighbourhoodSize; r++) {
                    br = false;

                    if (HoughArray[t, r] > threshold) {
                        var peak = HoughArray[t, r];

                        for (var dx = -NeighbourhoodSize; dx <= NeighbourhoodSize; dx++) {
                            if (br) break;
                            for (var dy = -NeighbourhoodSize; dy <= NeighbourhoodSize; dy++) {
                                var dt = t + dx;
                                var dr = r + dy;
                                if (dt < 0)
                                    dt = dt + MaxTheta;
                                else if (dt >= MaxTheta) dt = dt - MaxTheta;
                                if (HoughArray[dt, dr] > peak) {
                                    br = true;
                                }
                            }
                        }
                        
                        var theta = t * ThetaStep;

                        unfilteredLines.Add(new HoughLine(theta, r));
                    }
                }
            }

            var filteredLines = FilterHoughLines(unfilteredLines);
            
            PlotHoughLines(filteredLines, Colors.Blue, 1);
            return filteredLines;
        }
        
        private static List<HoughLine> FilterHoughLines(List<HoughLine> unfilteredLines) {
            var filteredLines = new List<HoughLine>();
            var analyzedLines = new List<HoughLine>();
            
            var allIntersections = new List<GeometryPoint>();

            foreach (var houghLine0 in unfilteredLines.ToArray()) {
                if (analyzedLines.Contains(houghLine0)) continue;
                var similarLines = new List<HoughLine> {
                    houghLine0
                };

                foreach (var houghLine1 in unfilteredLines.ToArray()) {
                    if (houghLine1 == houghLine0) continue;
                    if (analyzedLines.Contains(houghLine1)) continue;

                    var linesSimilar = Geometry.AreLinesSimilar(houghLine0, houghLine1, _polyline.Bounds);

                    if (linesSimilar) {
                        similarLines.Add(houghLine1);
                    } else {
                        var intersection = Geometry.GetLineIntersection(new GeometryLine(houghLine0, _polyline.Bounds), 
                            new GeometryLine(houghLine1, _polyline.Bounds));
                        
                        // no intersection in euclidean space
                        if (intersection == null) continue;
                        if (double.IsNaN(intersection.X) || double.IsNaN(intersection.Y)) continue;
                        
                        intersection.X += _polyline.Bounds.X;
                        intersection.Y += _polyline.Bounds.Y;
                        
                        allIntersections.Add(intersection);
                    }
                }
                
                analyzedLines.AddRange(similarLines);
            }

            /* var pointsClustered = */ Geometry.GetPointsClustered(allIntersections, _polyline.Bounds.AddMargin(50));

            // foreach (var geometryPoint in pointsClustered) {
            //     var frame = new Frame {
            //         Width = 6,
            //         Height = 6,
            //         Background = new SolidColorBrush(Colors.Black)
            //     };
            //
            //     App.EditorScreen.AddAbsoluteOverlayElement(frame, geometryPoint.ToPoint());
            // }
            
            return filteredLines;
        }

        public static HoughLine GetAverageHoughLine(List<HoughLine> lines) {
            var averageTheta = lines.Sum(line => line.Theta) / lines.Count;
            var averageRadius = lines.Sum(line => line.R) / lines.Count;
            
            return new HoughLine(averageTheta, averageRadius);
        }
        
        public static void PlotHoughLines(IEnumerable<HoughLine> houghLines, Color color, double thickness = 3.0) {
            var lines = houghLines.Select(line => new GeometryLine(line, _polyline.Bounds)).ToList();

            MainThread.BeginInvokeOnMainThread(() => {
                foreach (var (p0, p1) in lines) {
                    var (x0, y0) = p0;
                    var (x1, y1) = p1;
                    
                    App.EditorScreen.AddAbsoluteOverlayElement(new Line {
                        Stroke = new SolidColorBrush(color),
                        StrokeThickness = thickness,
                        X1 = _polyline.Bounds.X + x0,
                        X2 = _polyline.Bounds.X + x1,
                        Y1 = _polyline.Bounds.Y + y0,
                        Y2 = _polyline.Bounds.Y + y1,
                    });
                }
            });
        }

        public int GetHighestValue() {
            var max = 0;
            for (var t = 0; t < MaxTheta; t++) {
                for (var r = 0; r < DoubleHeight; r++) {
                    if (HoughArray[t, r] > max) {
                        max = HoughArray[t, r];
                    }
                }
            }

            return max;
        }

        public void GetHoughArrayImage() {
            var max = GetHighestValue();
            for (var t = 0; t < MaxTheta; t+=5) {
                for (int r = 0; r < DoubleHeight; r+=5) {
                    double value = 255 * ((double) HoughArray[t, r]) / max;
                    int v = 255 - (int) value;

                    var cl = Colors.Red;
                    cl = cl.AddLuminosity(v / 255f);

                    var frame = new Frame {
                        Width = 5,
                        Height = 5,
                        Background = new SolidColorBrush(cl)
                    };

                    Canvas.SetLeft(frame, r);
                    Canvas.SetTop(frame, t);

                    App.EditorScreen.AddAbsoluteOverlayElement(frame);
                }
            }
        }
    }
}