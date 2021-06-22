using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Helpers;

namespace AINotes.Helpers.Geometry {
    public static class Geometry {
        // maximum double value represents infinity
        static double INF = double.MaxValue;

        // Given three collinear points p, q, r,  
        // the function checks if point q lies 
        // on line segment 'pr' 
        public static bool OnSegment(GeometryPoint p, GeometryPoint q, GeometryPoint r) {
            if (q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) && q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y)) {
                return true;
            }

            return false;
        }

        // To find orientation of ordered triplet (p, q, r). 
        // The function returns following values 
        // 0 --> p, q and r are collinear 
        // 1 --> Clockwise 
        // 2 --> Counterclockwise 
        public static int Orientation(GeometryPoint p, GeometryPoint q, GeometryPoint r) {
            var val = (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);

            if (val == 0) {
                return 0; // collinear 
            }

            return val > 0 ? 1 : 2; // clockwise or counterclockwise 
        }

        // The function that returns true if  
        // line segment 'p1q1' and 'p2q2' intersect. 
        public static bool DoIntersect(GeometryPoint p1, GeometryPoint q1, GeometryPoint p2, GeometryPoint q2) {
            // Find the four orientations needed for  
            // general and special cases 
            var o1 = Orientation(p1, q1, p2);
            var o2 = Orientation(p1, q1, q2);
            var o3 = Orientation(p2, q2, p1);
            var o4 = Orientation(p2, q2, q1);

            // General case 
            if (o1 != o2 && o3 != o4) {
                return true;
            }

            // Special Cases 
            // p1, q1 and p2 are collinear and 
            // p2 lies on segment p1q1 
            if (o1 == 0 && OnSegment(p1, p2, q1)) {
                return true;
            }

            // p1, q1 and p2 are collinear and 
            // q2 lies on segment p1q1 
            if (o2 == 0 && OnSegment(p1, q2, q1)) {
                return true;
            }

            // p2, q2 and p1 are collinear and 
            // p1 lies on segment p2q2 
            if (o3 == 0 && OnSegment(p2, p1, q2)) {
                return true;
            }

            // p2, q2 and q1 are collinear and 
            // q1 lies on segment p2q2 
            if (o4 == 0 && OnSegment(p2, q1, q2)) {
                return true;
            }

            // Doesn't fall in any of the above cases 
            return false;
        }

        // Returns true if the point p lies  
        // inside the polygon[] with n vertices 
        public static bool IsInsidePolygon(GeometryPolyline polyline, GeometryPoint p) {
            // There must be at least 3 vertices in polygon[] 
            if (polyline.Points.Count < 3) {
                return false;
            }

            // Create a point for line segment from p to infinite 
            var extreme = new GeometryPoint(INF, p.Y);

            // Count intersections of the above line  
            // with sides of polygon 
            int count = 0, i = 0;
            do {
                var next = (i + 1) % polyline.Points.Count;

                // Check if the line segment from 'p' to  
                // 'extreme' intersects with the line  
                // segment from 'polygon[i]' to 'polygon[next]' 
                if (DoIntersect(polyline.Points[i], polyline.Points[next], p, extreme)) {
                    // If the point 'p' is colinear with line  
                    // segment 'i-next', then check if it lies  
                    // on segment. If it lies, return true, otherwise false 
                    if (Orientation(polyline.Points[i], p, polyline.Points[next]) == 0) {
                        return OnSegment(polyline.Points[i], p, polyline.Points[next]);
                    }

                    count++;
                }

                i = next;
            } while (i != 0);

            // Return true if count is odd, false otherwise 
            return count % 2 == 1; // Same as (count%2 == 1) 
        }
        
        // returns center of circle defined by three points (p0, p1, p2)
        public static GeometryPoint CalculateCircleCenter(GeometryPoint p0, GeometryPoint p1, GeometryPoint p2) {
            var (x0, y0) = p0.GetPosition();
            var (x1, y1) = p1.GetPosition();
            var (x2, y2) = p2.GetPosition();
            if (x1 - x0 == 0 || x2 - x1 == 0 || y1 - y0 == 0 || y2 - y1 == 0) return new GeometryPoint(0, 0);
        
            var center = new GeometryPoint();
            var ma = (y1 - y0) / (x1 - x0);
            var mb = (y2 - y1) / (x2 - x1);
            center.X = (ma * mb * (y0 - y2) + mb * (x0 - x1) - ma * (x1 + x2)) / (2 * (mb - ma));
            center.Y = -1 / ma * (center.Y - (x0 + x1) * 0.5) + (y0 + y1) * 0.5;
            return center;
        }

        // returns intersection position of two
        // lines in 2D
        public static GeometryPoint GetIntersection(int x0, int y0, int x1, int y1, int x2, int y2, int x3, int y3) {
            try {
                var px = ((x0 * y1 - y0 * x1) * (x2 - x3) - (x0 - x1) * (x2 * y3 - y2 * x3)) / ((x0 - x1) * (y2 - y3) - (y0 - y1) * (x2 - x3));
                var py = ((x0 * y1 - y0 * x1) * (y2 - y3) - (y0 - y1) * (x2 * y3 - y2 * x3)) / ((x0 - x1) * (y2 - y3) - (y0 - y1) * (x2 - x3));
                return new GeometryPoint(px, py);
            } catch (Exception) {
                // ignored
            }
        
            return GeometryPoint.NaP();
        }
        
        public static GeometryPoint GetIntersection(GeometryPoint p0, GeometryPoint p1, GeometryPoint p2, GeometryPoint p3) {
            try {
                var px = ((p0.X * p1.Y - p0.Y * p1.X) * (p2.X - p3.X) - (p0.X - p1.X) *
                    (p2.X * p3.Y - p2.Y * p3.X)) / ((p0.X - p1.X) * (p2.Y - p3.Y) - (p0.Y - p1.Y) * (p2.X - p3.X));
                
                var py = ((p0.X * p1.Y - p0.Y * p1.X) * (p2.Y - p3.Y) - (p0.Y - p1.Y) * 
                    (p2.X * p3.Y - p2.Y * p3.X)) / ((p0.X - p1.X) * (p2.Y - p3.Y) - (p0.Y - p1.Y) * (p2.X - p3.X));
                
                return new GeometryPoint(px, py);
            } catch (Exception) {
                // ignored
            }

            return GeometryPoint.NaP();
        }
        
        public static GeometryPoint GetIntersection(GeometryStraight geometryStraight0, GeometryStraight geometryStraight1) {
            try {
                var x = (geometryStraight0.B - geometryStraight1.B) / (geometryStraight1.M - geometryStraight0.M);
                var y = geometryStraight0.M * x + geometryStraight0.B;
                return new GeometryPoint(x, y);
            } catch (Exception) {
                // ignored
            }
        
            return GeometryPoint.NaP();
        }
        
        // returns whether or not two lines intersect in 2D space
        public static bool HasIntersection(GeometryPoint lineStart0, GeometryPoint lineEnd0, GeometryPoint lineStart1, GeometryPoint lineEnd1) {
            // use two-dimensional vectors to check for intersection
            var p = new[] { lineStart0.X, lineStart0.Y, lineEnd0.X - lineStart0.X, lineEnd0.Y - lineStart0.Y, lineStart1.X, lineStart1.Y, lineEnd1.X - lineStart1.X, lineEnd1.Y - lineStart1.Y };

            if (p[6] * p[3] - p[7] * p[2] == 0) return false;
            
            var r = (p[0] * p[3] - p[1] * p[2] - p[4] * p[3] + p[5] * p[2]) / (p[6] * p[3] - p[7] * p[2]);
            if (r < 0 || r > 1) return false;
            var s = (p[4] + p[6] * r - p[0]) / p[2];
            return !(s < 0) && !(s > 1);
        }
        
        public static bool HasIntersection(GeometryLine geometryLine0, GeometryLine geometryLine1) {
            return HasIntersection(geometryLine0.P0, geometryLine0.P1, geometryLine1.P0, geometryLine1.P1);
        }
        
        public static bool HasIntersection(GeometryStraight geometryStraight0, GeometryStraight geometryStraight1) {
            return GetIntersection(geometryStraight0, geometryStraight0) != GeometryPoint.NaP();
        }
        
        // returns whether or not two polylines intersect in 2D space
        public static bool FindPolyLineIntersection(GeometryPoint[] polyline0, GeometryPoint[] polyline1) {
            GeometryPoint lastPoint0 = null;
            GeometryPoint lastPoint1 = null;
            
            // iterating through every point of the first polyline
            foreach (var inkPoint0 in polyline0) {
                if (lastPoint0 == null) {
                    lastPoint0 = inkPoint0;
                    continue;
                }
                
                // iterating through every point of the second polyline
                foreach (var inkPoint1 in polyline1) {
                    if (lastPoint1 == null) {
                        lastPoint1 = inkPoint1;
                        continue;
                    }

                    // checking for intersection between the two pieces of the two polylines
                    // that are generated through the iteration
                    if (HasIntersection(lastPoint0, inkPoint0, lastPoint1, inkPoint1)) return true;
                    lastPoint1 = inkPoint1;
                }

                lastPoint0 = inkPoint0;
            }

            return false;
        }
        
        // returns angle (x-axis, p0, p1)
        public static double GetAngle(GeometryPoint p0, GeometryPoint p1) {
            const double rad2Deg = 180.0 / Math.PI;
            var (sX, xY) = p0.GetPosition();
            var (eX, eY) = p1.GetPosition();
            return Math.Atan2(xY - eY, eX - sX) * rad2Deg;
        }
        
        public static double GetAngle(GeometryPoint p0, GeometryPoint p1, GeometryPoint p2) {
            var ab = GetDistance(p0, p1);
            var bc = GetDistance(p1, p2);
            var ac = GetDistance(p0, p2);

            var cosB = Math.Pow(ac, 2) - Math.Pow(ab, 2) - Math.Pow(bc, 2);
            cosB = cosB / (2 * ab * bc);

            return 180 - Math.Acos(cosB) * 180 / Math.PI;
        }

        public static double GetDistance(GeometryPoint p0, GeometryPoint p1) {
            return GetHypotenuse(p0.X - p1.X, p0.Y - p1.Y);
        }

        // returns the dot product of two given 2D
        // vectors after normalizing them
        public static double GetDotProductNormalized(GeometryPoint p0, GeometryPoint p1) {
            var (x0, y0) = p0.GetPosition();
            var (x1, y1) = p1.GetPosition();
            
            var l0 = Math.Sqrt(Math.Pow(x0, 2) + Math.Pow(y0, 2));
            var l1 = Math.Sqrt(Math.Pow(x1, 2) + Math.Pow(y1, 2));
            x0 /= l0;
            y0 /= l0;
            x1 /= l1;
            y1 /= l1;
            return x0 * x1 + y0 * y1;
        }
        
        // returns area of a given polygon
        public static double GetPolygonArea(GeometryPoint[] polygon) {
            var polygonAsList = polygon.ToList();
            polygonAsList.Add(polygon.First());
            
            return Math.Abs(polygonAsList.Take(polygonAsList.Count - 1)
                .Select((p, i) => (polygon[i + 1].X - p.X) * (polygon[i + 1].Y + p.Y))
                .Sum() / 2);
        }
        
        public static double GetPolygonArea(GeometryPolyline polygon) {
            return GetPolygonArea(polygon.Points.ToArray());
        }
        
        // returns nearest point to refPoint out of set
        public static GeometryPoint GetClosestPoint(GeometryPoint refPoint, IEnumerable<GeometryPoint> points) {
            var shortestDistancePoint = new GeometryPoint(0, 0);
            var shortestDistance = double.MaxValue;
        
            foreach (var p in points) {
                var (x, y) = refPoint.GetPosition();
                var dst = Math.Sqrt(Math.Pow(p.X - x, 2) + Math.Pow(p.Y - y, 2));
                if (!(dst < shortestDistance)) continue;
                shortestDistance = dst;
                shortestDistancePoint = p;
            }
        
            return shortestDistancePoint;
        }

        // calculate hypotenuse or cathetus (pythagorean theorem)
        public static double GetHypotenuse(double a, double b) {
            return Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
        }

        public static double GetCathetus(double a, double c) {
            return Math.Sqrt(Math.Pow(c, 2) - Math.Pow(a, 2));
        }

        public static double[] Linspace(double startValue, double endValue, int numberOfPoints) {
            var parameterValues = new double[numberOfPoints];
            var increment = Math.Abs(startValue - endValue) / Convert.ToDouble(numberOfPoints - 1);
            
            //will keep a track of the numbers
            var j = 0;  
            var nextValue = startValue;
            for (var i = 0; i < numberOfPoints; i++) {
                parameterValues.SetValue(nextValue, j);
                j++;
                if (j > numberOfPoints) {
                    throw new IndexOutOfRangeException();
                }

                nextValue += increment;
            }

            return parameterValues;
        }

        public static GeometryPoint GetLineIntersection(Point x0, Point y0, Point x1, Point y1) {
            var a1 = y0.Y - x0.Y;
            var b1 = x0.X - y0.X;
            var c1 = a1 * x0.X + b1 * x0.Y;
 
            var a2 = y1.Y - x1.Y;
            var b2 = x1.X - y1.X;
            var c2 = a2 * x1.X + b2 * x1.Y;
 
            var delta = a1 * b2 - a2 * b1;
            
            return delta == 0 ? null : new GeometryPoint((b2 * c1 - b1 * c2) / delta, (a1 * c2 - a2 * c1) / delta);
        }
        
        public static GeometryPoint GetLineIntersection(GeometryLine line0, GeometryLine line1) {
            var (x0, y0) = line0;
            var (x1, y1) = line1;
            
            var a1 = y0.Y - x0.Y;
            var b1 = x0.X - y0.X;
            var c1 = a1 * x0.X + b1 * x0.Y;
 
            var a2 = y1.Y - x1.Y;
            var b2 = x1.X - y1.X;
            var c2 = a2 * x1.X + b2 * x1.Y;
 
            var delta = a1 * b2 - a2 * b1;
            
            return delta == 0 ? null : new GeometryPoint((b2 * c1 - b1 * c2) / delta, (a1 * c2 - a2 * c1) / delta);
        }
        
        // private static GeometryRectangle GetBoundsFromPoints(List<GeometryPoint> points) {
        //     var x = points.Min(point => point.X);
        //     var y = points.Min(point => point.Y);
        //     var width = points.Max(point => point.X) - x;
        //     var height = points.Max(point => point.Y) - y;
        //     
        //     return new GeometryRectangle(x, y, width, height);
        // }
        
        public static bool AreLinesSimilar(HoughLine houghLine0, HoughLine houghLine1, GeometryRectangle polylineBounds) {
            var intersection = GetLineIntersection(new GeometryLine(houghLine0, polylineBounds), new GeometryLine(houghLine1, polylineBounds));

            const double angleDeviation = 30;
            
            // no intersection in euclidean space
            if (intersection == null) return false;
            if (double.IsNaN(intersection.X) || double.IsNaN(intersection.Y)) return false;

            intersection.X += polylineBounds.X;
            intersection.Y += polylineBounds.Y;

            if (!polylineBounds.AddMargin(40).Contains(intersection)) return false;

            if (Math.Abs(houghLine0.Angle - houghLine1.Angle) < angleDeviation || Math.Abs(houghLine0.Angle - houghLine1.Angle) > 180 - angleDeviation) {
                // var frame = new Frame {
                //     Width = 6,
                //     Height = 6,
                //     Background = new SolidColorBrush(Colors.Green)
                // };
                //
                // App.EditorScreen.AddAbsoluteOverlayElement(frame, intersection.ToPoint());

                return true;
            } else {
                var frame = new Frame {
                    Width = 6,
                    Height = 6,
                    Background = new SolidColorBrush(Colors.Red)
                };
                
                App.EditorScreen.AddAbsoluteOverlayElement(frame, intersection.ToPoint());

                return false;
            }
        }

        public static List<GeometryPoint> GetPointsClustered(List<GeometryPoint> geometryPoints, GeometryRectangle bounds) {
            var results = new List<GeometryPoint>();

            const double maxDist = 40;

            foreach (var geometryPoint0 in geometryPoints.ToArray()) {
                var similarPoints = new List<GeometryPoint>();
                
                foreach (var geometryPoint1 in geometryPoints.ToArray()) {
                    if (geometryPoint0 == geometryPoint1) continue;

                    var dist = GetHypotenuse(geometryPoint0.X - geometryPoint1.X, geometryPoint0.Y - geometryPoint1.Y);
                    if (dist < maxDist) similarPoints.Add(geometryPoint1);
                }

                var averageX = similarPoints.Sum(point => point.X) / similarPoints.Count;
                var averageY = similarPoints.Sum(point => point.Y) / similarPoints.Count;

                var result = new GeometryPoint(averageX, averageY);
                if (!bounds.Contains(result)) continue;
                
                results.Add(result);
            }

            return results;
        }
    }
}