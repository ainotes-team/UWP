using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using AINotes.Helpers.Geometry;
using Helpers;
using Colors = Windows.UI.Colors;

namespace AINotes.Helpers.InkCanvas {
    // TODO: Improve Performance
    public static class InkConversion {
        // determines whether or not polyline is line
        // checks for each point in polyline whether is lies in a specified area defined by scope and outer points
        public static InkShape IsLine(GeometryPolyline polyline, double scope = 0) {
            if (polyline == null) throw new ArgumentNullException(nameof(polyline));
            if (scope == 0) scope = Convert.ToDouble(Preferences.InkConversionTolerance) * 5;
            var isLine = true;
            var firstPoint = polyline.Points.First();
            var lastPoint = polyline.Points.Last();

            Debug.Assert(firstPoint != null, nameof(firstPoint) + " != null");
            Debug.Assert(lastPoint != null, nameof(lastPoint) + " != null");
            var (leftPointX, leftPointY) = firstPoint.X < lastPoint.X ? firstPoint.GetPosition() : lastPoint.GetPosition();
            var (rightPointX, rightPointY) = firstPoint.X < lastPoint.X ? lastPoint.GetPosition() : firstPoint.GetPosition();

            var degree = Math.Atan2(rightPointY - leftPointY, rightPointX - leftPointX) * 180 / Math.PI;
            var hypotenuse = Math.Sqrt(2 * Math.Pow(scope, 2));
            
            var polygon = new GeometryPolyline(new[] {
                new GeometryPoint(leftPointX - Math.Cos(Math.PI * (degree - 45) / 180.0) * hypotenuse, leftPointY - Math.Sin(Math.PI * (degree - 45) / 180.0) * hypotenuse),
                new GeometryPoint(leftPointX - Math.Cos(Math.PI * (degree + 45) / 180.0) * hypotenuse, leftPointY - Math.Sin(Math.PI * (degree + 45) / 180.0) * hypotenuse),
                new GeometryPoint(rightPointX + Math.Cos(Math.PI * (degree - 45) / 180.0) * hypotenuse, rightPointY + Math.Sin(Math.PI * (degree - 45) / 180.0) * hypotenuse),
                new GeometryPoint(rightPointX + Math.Cos(Math.PI * (degree + 45) / 180.0) * hypotenuse, rightPointY + Math.Sin(Math.PI * (degree + 45) / 180.0) * hypotenuse)
            });

            const int probability = 1;

            var dst = Math.Sqrt(Math.Pow(rightPointX - leftPointX, 2) + Math.Pow(rightPointY - leftPointY, 2));

            var minDst = 150f / App.EditorScreen.ScrollZoom;
            
            try {
                if (polyline.Points.Any(point => !Geometry.Geometry.IsInsidePolygon(polygon, point)) || dst < minDst) {
                    isLine = false;
                }

                if (!Preferences.InkAdjustLines) return isLine ? new InkShape {
                    Polyline = new GeometryPolyline(new [] {
                        new GeometryPoint(firstPoint.X, firstPoint.Y),
                        new GeometryPoint(lastPoint.X, lastPoint.Y)
                    }),
                    Probability = probability,
                    ShapeType = ShapeType.Line
                } : null;
                if (!isLine) return null;
                
                // adjust line according to degree (0°, 45°, 90°)
                // checks if line is horizontal
                if (degree < 5 && degree > -5) {
                    var newY = (firstPoint.Y + lastPoint.Y) / 2;
                    return new InkShape {
                        Polyline = new GeometryPolyline(new [] {
                            new GeometryPoint(firstPoint.X, newY),
                            new GeometryPoint(lastPoint.X, newY)
                        }),
                        Probability = probability,
                        ShapeType = ShapeType.Line
                    };
                }

                // checks if line is vertical
                if (degree > 85 || degree < -85) {
                    var newX = (firstPoint.X + lastPoint.X) / 2;
                    
                    return new InkShape {
                        Polyline = new GeometryPolyline(new [] {
                            new GeometryPoint(newX, firstPoint.Y),
                            new GeometryPoint(newX, lastPoint.Y)
                        }),
                        Probability = probability,
                        ShapeType = ShapeType.Line
                    };
                }

                // checks for degree around 45°
                if (degree < 50 && degree > 40) {
                    var length = Math.Sqrt(Math.Pow(rightPointX - leftPointX, 2) + Math.Pow(leftPointY - rightPointY, 2));
                    var middleX = (rightPointX + leftPointX) / 2;
                    var middleY = (rightPointY + leftPointY) / 2;

                    var diffX = Math.Sqrt(Math.Pow(length / 2, 2) / 2);
                    var diffY = Math.Sqrt(Math.Pow(length / 2, 2) / 2);

                    return new InkShape {
                        Polyline = new GeometryPolyline(new [] {
                            new GeometryPoint(middleX - diffX, middleY - diffY),
                            new GeometryPoint(middleX + diffX, middleY + diffY)
                        }),
                        Probability = probability,
                        ShapeType = ShapeType.Line
                    };
                }

                // checks for degree around -45°
                if (!(degree < -40) || !(degree > -50))
                    return new InkShape {
                        Polyline = new GeometryPolyline(new [] {
                            new GeometryPoint(firstPoint.X, firstPoint.Y),
                            new GeometryPoint(lastPoint.X, lastPoint.Y)
                        }),
                        Probability = probability,
                        ShapeType = ShapeType.Line
                    };
                {
                    var length = Math.Sqrt(Math.Pow(rightPointX - leftPointX, 2) + Math.Pow(leftPointY - rightPointY, 2));
                    var middleX = (rightPointX + leftPointX) / 2;
                    var middleY = (rightPointY + leftPointY) / 2;

                    var diffX = Math.Sqrt(Math.Pow(length / 2, 2) / 2);
                    var diffY = Math.Sqrt(Math.Pow(length / 2, 2) / 2);

                    return new InkShape {
                        Polyline = new GeometryPolyline(new [] {
                            new GeometryPoint(middleX - diffX, middleY + diffY),
                            new GeometryPoint(middleX + diffX, middleY - diffY)
                        }),
                        Probability = probability,
                        ShapeType = ShapeType.Line
                    };
                }
            }
            
            catch (Exception ex) {
                Logger.Log("[InkConverter]", "Exception in RecognizeLines: ", ex.ToString(), logLevel: LogLevel.Error);
                return new InkShape();
            }
        }
        
        // determines whether or not poly line is polygon
        // ReSharper disable once UnusedMember.Local
        private static InkShape IsPolygon(GeometryPolyline polyline) {
            if (polyline.Count == 0) return null;

            var (_, _, strokeWidth, strokeHeight) = polyline.Bounds.GetDimensions();

            var inkShape = new InkShape {
                ShapeType = ShapeType.None,
                Probability = 0
            };
            
            // needs more than 30 elements to calculate properly
            if (polyline.Count < 30) return null;
            
            if (!polyline.IsAlmostClosed()) return null;
            
            // checking for possible corners by potting degree value in between two points - iteration
            var possibleCorners = polyline.GetPossibleCorners();

            // creating clusters to concentrate on relevant corners
            var clustered = Cluster(possibleCorners);
            if (clustered.Count == 0) return null;
            var clusteredX = clustered.Min(point => point.X);
            var clusteredY = clustered.Min(point => point.Y);
            var clusteredWidth = clustered.Max(point => point.X) - clusteredX;
            var clusteredHeight = clustered.Max(point => point.Y) - clusteredY;
            
            var percentageRectFilling = clusteredWidth * clusteredHeight / (strokeWidth * strokeHeight);
            
            // debug
            foreach (var point in possibleCorners) {
                inkShape.Polyline.AddPoint(point.X, point.Y);

                var frame = new Frame {
                    Background = new SolidColorBrush(Colors.Red),
                    CornerRadius = new CornerRadius(0, 10, 10, 10),
                    Width = 20,
                    Height = 20,
                };
                
                Canvas.SetLeft(frame, point.X);
                Canvas.SetTop(frame, point.Y);
                
                App.EditorScreen.AddAbsoluteOverlayElement(frame);
            }

            // first clustered
            var (x, y) = clustered.First().GetPosition();
            inkShape.Polyline.AddPoint(x, y);

            inkShape = RemovePolygonCornerOutliers(inkShape, 0);
            
            if (clustered.Count > 8 || percentageRectFilling < .8) return null;
            
            inkShape = AlignPolygon(inkShape);

            var threshold = Preferences.ConversionThreshold / 100;
            inkShape.Probability = threshold < .93 ? threshold + .001 : .931;
            if (inkShape.Polyline.Count < 5) inkShape.Probability = .95;
            inkShape.ShapeType = ShapeType.Polygon;
            
            return inkShape;
        }
        
        // determines whether or not polyline is ellipse
        // WARNING: does currently only support ellipses without specified angle
        // ReSharper disable once UnusedMember.Local
        private static InkShape IsEllipse(GeometryPolyline stroke) {
            // checks for the polyline to be closed
            if (!stroke.IsAlmostClosed()) return null;

            var (approximateX, approximateY, approximateWidth, approximateHeight) = stroke.Bounds.GetDimensions();

            var minSize = 150f / App.EditorScreen.ScrollZoom;
            
            if (approximateWidth < minSize || approximateHeight < minSize) return null;

            double probability = 0;
            
            // center and foci of the ellipse
            var approximateCenter = (approximateX + approximateWidth / 2, approximateY + approximateHeight / 2);
            double approximateF;

            GeometryPoint approximateFocus0;
            GeometryPoint approximateFocus1;
            
            var polyline = new GeometryPolyline();

            double absR;

            // horizontal / vertical aligned ellipse
            if (approximateWidth > approximateHeight) {
                approximateF = Math.Sqrt(Math.Pow(approximateWidth * 1f / 2, 2) - Math.Pow(approximateHeight * 1f / 2, 2));
                
                approximateFocus0 = new GeometryPoint(approximateCenter.Item1 - approximateF, approximateCenter.Item2);
                approximateFocus1 = new GeometryPoint(approximateCenter.Item1 + approximateF, approximateCenter.Item2);

                absR = approximateWidth;
            } else {
                approximateF = Math.Sqrt(Math.Pow(approximateHeight * 1f / 2, 2) - Math.Pow(approximateWidth * 1f / 2, 2));
                
                approximateFocus0 = new GeometryPoint(approximateCenter.Item1, approximateCenter.Item2 - approximateF);
                approximateFocus1 = new GeometryPoint(approximateCenter.Item1, approximateCenter.Item2 + approximateF);

                absR = approximateHeight;
            }
            
            foreach (var point in stroke.Points) {
                var f0d = Math.Sqrt(Math.Pow(point.X - approximateFocus0.X, 2) + Math.Pow(point.Y - approximateFocus0.Y, 2));
                var f1d = Math.Sqrt(Math.Pow(point.X - approximateFocus1.X, 2) + Math.Pow(point.Y - approximateFocus1.Y, 2));
                var d = f0d + f1d;
                var error = Math.Abs(d - absR) / absR;
                probability += 1 - error;
                if (error > .2) probability -= .5;
            }

            probability /= stroke.Count;

            var stepSize = (float) (Math.PI / stroke.Count * 2);

            var factor = approximateWidth * 1f / approximateHeight;
            if (Math.Abs(factor - 1) < .2) {
                factor = 1f;
            }
            var radius = approximateHeight / 2;

            probability -= Math.Abs(1 - factor) * .02;
            Logger.Log("Ellipse Probability: ", probability);

            for (float i = 0; i <= 2 * Math.PI + 1; i += stepSize) {
                var x = approximateCenter.Item1 + radius * Math.Cos(i) * factor;
                var y = approximateCenter.Item2 + radius * Math.Sin(i);
                    
                polyline.AddPoint(x, y);
            }

            return new InkShape {
                Polyline = polyline,
                Probability = probability,
                ShapeType = ShapeType.Ellipse
            };
        }

        private const int MinSize = 150;

        // returns analyzed polyline in form of InkShape
        public static InkShape AnalyzePolyline(GeometryPolyline polyline) {
            var analysisResults = new Dictionary<double, InkShape>();

            // possible conversion shapes: polygon, ellipse, line
            
            // var ellipse = IsEllipse(polyline);
            // if (ellipse != null && !analysisResults.ContainsKey(ellipse.Probability)) analysisResults.Add(ellipse.Probability, ellipse);
            //
            // var polygon = IsPolygon(polyline);
            // if (polygon != null && !analysisResults.ContainsKey(polygon.Probability)) analysisResults.Add(polygon.Probability, polygon);
            
            var line = IsLine(polyline);
            if (line != null && !analysisResults.ContainsKey(line.Probability)) analysisResults.Add(line.Probability, line);

            // return empty InkShape when error occurs
            if (analysisResults.Count == 0) return new InkShape();
            
            var highestVote = analysisResults.Max(pair => pair.Key);
            if (highestVote * 100 < Preferences.ConversionThreshold) {
                return new InkShape { ShapeType = ShapeType.None };
            }

            var shape = analysisResults[highestVote];
            var (_, _, shapeWidth, shapeHeight) = shape.Polyline.Bounds.GetDimensions();

            var minLength = MinSize / App.EditorScreen.ScrollZoom;

            if (shapeWidth < minLength && shapeHeight < minLength) {
                return new InkShape { ShapeType = ShapeType.None };
            } else {
                return shape;
            }
        }

        private const int OutlierAngle = 140;

        // returns removed outliers from polygon
        private static InkShape RemovePolygonCornerOutliers(InkShape polygon, int iteration, List<GeometryPoint> outliers = null) {
            if (iteration == polygon.Polyline.Count) {
                var newPolygonPoints = polygon.Polyline.Points.Where(point => {
                    Debug.Assert(outliers != null, nameof(outliers) + " != null");
                    return !outliers.Contains(point);
                }).ToList();
                polygon.Polyline.Points = newPolygonPoints;
                return polygon;
            }
            if (outliers == null) outliers = new List<GeometryPoint>();

            foreach (var middlePoint in from p0 in polygon.Polyline.Points
                    let index = polygon.Polyline.Points.IndexOf(p0) where index + 1 <= polygon.Polyline.Count - 2 
                    let p1 = polygon.Polyline.Points[index + 1] 
                    let p2 = polygon.Polyline.Points[index + 2] 
                    let simplifiedLine = new [] {
                        new GeometryPoint(p0.X, p0.Y),
                        new GeometryPoint(p1.X, p1.Y),
                        new GeometryPoint(p2.X, p2.Y)
                    }
                    where !outliers.Contains(p0) 
                    let angle = Math.Abs(Geometry.Geometry.GetAngle(simplifiedLine[0], simplifiedLine[1], simplifiedLine[2]))
                    where angle > OutlierAngle select p1) {
                outliers.Add(middlePoint);
            }
            
            // mixing list so first item changes every iteration: for instance 1 -> 4 (pr0); 2 -> 5 (pr1)
            var pl = polygon.Polyline.Points.ToList();
            var pr0 = pl[0];
            var pr1 = pl[1];
            pl.Remove(pr0);
            pl.Remove(pr1);
            pl.Add(pr0);
            pl.Add(pr1);
            return RemovePolygonCornerOutliers(polygon, iteration + 1, outliers);
        }

        // aligns polygon to background
        // grid and by degree
        private static InkShape AlignPolygon(InkShape polygon) {
            // comparison threshold for opposite side lengths
            const int threshold = 200;
            // min dot product for normalized vectors
            const double dotP = .15;
            
            Logger.Log("Align Polygon with", polygon.Polyline.Count, "Corners");
            
            // rectangle requires 4 (5 - 1 since first and last point match each other) corners / sides
            if (polygon.Polyline.Count == 5) {
                Logger.Log("Polygon has 4 corners");
                
                // polygon corners
                var a = polygon.Polyline.Points[0];
                var b = polygon.Polyline.Points[1];
                var c = polygon.Polyline.Points[2];
                var d = polygon.Polyline.Points[3];

                // polygon side vectors
                var ab = new GeometryPoint(b.X - a.X, b.Y - a.Y);
                var bc = new GeometryPoint(c.X - b.X, c.Y - b.Y);
                var cd = new GeometryPoint(d.X - c.X, d.Y - c.Y);
                var da = new GeometryPoint(a.X - d.X, a.Y - d.Y);

                // polygon side lengths 
                var lab = Math.Sqrt(Math.Pow(ab.X, 2) + Math.Pow(ab.Y, 2));
                var lbc = Math.Sqrt(Math.Pow(bc.X, 2) + Math.Pow(bc.Y, 2));
                var lcd = Math.Sqrt(Math.Pow(cd.X, 2) + Math.Pow(cd.Y, 2));
                var lda = Math.Sqrt(Math.Pow(da.X, 2) + Math.Pow(da.Y, 2));

                // check for the lengths of opposite sides
                if (Math.Abs(lab - lcd) < threshold && Math.Abs(lbc - lda) < threshold) {
                    Logger.Log("Side lengths almost equal");
                    
                    // dot products for each intersection have a degree reference
                    var abc = Math.Abs(Geometry.Geometry.GetDotProductNormalized(new GeometryPoint(ab.X, ab.Y),
                        new GeometryPoint(bc.X, bc.Y)));
                    
                    var bcd = Math.Abs(Geometry.Geometry.GetDotProductNormalized(new GeometryPoint(bc.X, bc.Y), 
                        new GeometryPoint(cd.X, cd.Y)));
                    
                    var cda = Math.Abs(Geometry.Geometry.GetDotProductNormalized(new GeometryPoint(cd.X, cd.Y),
                        new GeometryPoint(da.X, da.Y)));
                    
                    var dab = Math.Abs(Geometry.Geometry.GetDotProductNormalized(new GeometryPoint(da.X, da.Y), 
                        new GeometryPoint(ab.X, ab.Y)));

                    // checking for all angles to be near 90 degrees => similar to rectangle
                    if (abc < dotP && bcd < dotP && cda < dotP && dab < dotP) {
                        // recalculating bc for orthogonality
                        var factor = bc.X < 0 ? -1 : 1;
                        bc.X = Math.Abs(bc.Y * ab.Y / ab.X) * factor;

                        // orig dist b - c
                        var dstBc = Math.Sqrt(Math.Pow(b.X - c.X, 2) + Math.Pow(b.Y - c.Y, 2));

                        lbc = Math.Sqrt(Math.Pow(bc.X, 2) + Math.Pow(bc.Y, 2));
                        
                        bc.X /= (float) lbc;
                        bc.Y /= (float) lbc;

                        bc.X *= (float) dstBc;
                        bc.Y *= (float) dstBc;

                        lbc = dstBc;

                        // grid alignment
                        var horV = new GeometryPoint(1, 0);
                        var verV = new GeometryPoint(0, 1);
                        if (Math.Abs(Geometry.Geometry.GetDotProductNormalized(horV, new GeometryPoint(ab.X, ab.Y))) < dotP) {
                            // ab is almost vertical
                            var fab1 = ab.X < 0 ? -1 : 1;
                            var fab2 = ab.Y < 0 ? -1 : 1;
                            ab.X = (float) (verV.X * lab * fab1);
                            ab.Y = (float) (verV.Y * lab * fab2);
                            
                            var fbc1 = bc.X < 0 ? -1 : 1;
                            var fbc2 = bc.Y < 0 ? -1 : 1;
                            bc.X = (float) (horV.X * lbc * fbc1);
                            bc.Y = (float) (horV.Y * lbc * fbc2);

                            polygon.AlignToGrid = true;
                        } else if (Math.Abs(Geometry.Geometry.GetDotProductNormalized(verV, new GeometryPoint(ab.X, ab.Y))) < dotP) {
                            // ab is almost horizontal
                            var fab1 = ab.X < 0 ? -1 : 1;
                            var fab2 = ab.Y < 0 ? -1 : 1;
                            ab.X = (float) (horV.X * lab * fab1);
                            ab.Y = (float) (horV.Y * lab * fab2);

                            var fbc1 = bc.X < 0 ? -1 : 1;
                            var fbc2 = bc.Y < 0 ? -1 : 1;
                            bc.X = (float) (verV.X * lbc * fbc1);
                            bc.Y = (float) (verV.Y * lbc * fbc2);

                            polygon.AlignToGrid = true;
                        }
                        
                        polygon.Polyline = new GeometryPolyline(new [] {
                            a,
                            new GeometryPoint(a.X + ab.X, a.Y + ab.Y), 
                            new GeometryPoint(a.X + ab.X + bc.X, a.Y + ab.Y + bc.Y),
                            new GeometryPoint(a.X + bc.X, a.Y + bc.Y),
                            a
                        });
                    }
                }
            }

            if (polygon.AlignToGrid) polygon.Polyline.AlignToGrid();
            
            return polygon;
        }

        // returns list of clustered set of points
        public static List<GeometryPoint> Cluster(List<GeometryPoint> points) {
            if (points.Count == 0) return points;

            var handledPoints = new List<GeometryPoint>();
            var clusters = new List<Cluster>();

            var x = points.Min(point => point.X);
            var y = points.Min(point => point.Y);
            var width = points.Max(point => point.X) - x;
            var height = points.Max(point => point.Y) - y;

            var smallerSideLength = Math.Max(width, height);

            // initialize
            var initialCluster = new Cluster();
            initialCluster.Points.Add(points.FirstOrDefault());
            clusters.Add(initialCluster);
            handledPoints.Add(points.FirstOrDefault());

            foreach (var point in points) {
                if (handledPoints.Contains(point)) continue;
                var nearestCluster = GetNearestCluster(point, new [] {
                    clusters.FirstOrDefault(),
                    clusters.LastOrDefault()
                });
                var ctr = nearestCluster.GetCenter();
                var (cX, xY) = ctr.GetPosition();
                var dst = Math.Sqrt(Math.Pow(point.X - cX, 2) + Math.Pow(point.Y - xY, 2));
                if (dst < smallerSideLength / 4) {
                    nearestCluster.Points.Add(point);
                    handledPoints.Add(point);
                } else {
                    var newCluster = new Cluster();
                    newCluster.Points.Add(point);
                    clusters.Add(newCluster);
                }
            }

            return clusters.Where(i => i != null).ToList().ConvertAll(input => input.GetCenter());
        }

        // returns nearest cluster to given point p
        private static Cluster GetNearestCluster(GeometryPoint p, IEnumerable<Cluster> clusters) {
            var shortestDistanceCluster = new Cluster();
            var shortestDistance = double.MaxValue;

            foreach (var cluster in clusters) {
                var ctr = cluster.GetCenter();
                var (x, y) = ctr.GetPosition();
                var (pX, pY) = p.GetPosition();
                
                // pythagorean theorem
                var dst = Math.Sqrt(Math.Pow(pX - x, 2) + Math.Pow(pY - y, 2));
                if (!(dst < shortestDistance)) continue;
                // updating shortest distance if dst is smaller
                shortestDistance = dst;
                shortestDistanceCluster = cluster;
            }

            return shortestDistanceCluster;
        }
    }

    public class Cluster {
        public readonly List<GeometryPoint> Points = new List<GeometryPoint>();
        
        // returns center of cluster / Points
        public GeometryPoint GetCenter() {
            if (Points.Count == 0) throw new Exception("Points cannot be empty.");
            var totalX = 0.0;
            var totalY = 0.0;
            foreach (var point in Points) {
                totalX += point.X;
                totalY += point.Y;
            }

            var centerX = totalX / Points.Count;
            var centerY = totalY / Points.Count;
            
            return new GeometryPoint(centerX, centerY);
        }
    }

    // InkShape Types
    public enum ShapeType {
        Polygon,
        Ellipse,
        Line,
        None
    }

    // container for shape type, conversion probability and points
    public class InkShape {
        // kind of shape (ShapeType)
        public ShapeType ShapeType = ShapeType.None;
        
        // points that define the shape
        public GeometryPolyline Polyline = new GeometryPolyline();
        
        // conversion probability
        public double Probability;
        
        // is rectangle -> shall be aligned to grid
        public bool AlignToGrid = true;
    }
}