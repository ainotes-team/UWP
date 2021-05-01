using System;
using System.Collections.Generic;
using System.Linq;
using AINotes.Helpers.InkCanvas;

namespace AINotes.Helpers.Geometry {
    public class GeometryPolyline {
        public List<GeometryPoint> Points;

        private GeometryRectangle _bounds;

        public GeometryRectangle Bounds {
            get {
                if (Points.Count == 0) throw new InvalidOperationException("GeometryPolyline.Points does not contain any points");
                
                _bounds.X = Points.Min(point => point.X);
                _bounds.Y = Points.Min(point => point.Y);

                _bounds.Width = Points.Max(point => point.X) - _bounds.X;
                _bounds.Height = Points.Max(point => point.Y) - _bounds.Y;

                return _bounds;
            }
            set => _bounds = value;
        }

        public int Count => Points.Count;

        public GeometryPolyline(List<GeometryPoint> polyline) {
            Points = polyline.ToList();
            _bounds = new GeometryRectangle();
        }

        public GeometryPolyline(GeometryPoint[] polyline) {
            Points = polyline.ToList();
            _bounds = new GeometryRectangle();
        }

        public GeometryPolyline() {
            Points = new List<GeometryPoint>();
            _bounds = new GeometryRectangle();
        }
        
        public void AlignToGrid() {
            foreach (var point in Points) {
                point.AlignToGrid();
            }
        }
        
        // returns whether or not polyline is closed
        public bool IsAlmostClosed() {
            var (fX, fY) = Points.First().GetPosition();
            var (lX, lY) = Points.Last().GetPosition();

            var dst = Math.Sqrt(Math.Pow(lX - fX, 2) + Math.Pow(lY - fY, 2));
            return dst < 100;
        }

        public void AddPoint(double x, double y) {
            Points.Add(new GeometryPoint(x, y));
        }

        public void AddPoint(GeometryPoint point) {
            Points.Add(point);
        }

        public InkShape GetShape() {
            return InkConversion.AnalyzePolyline(this);
        }

        public List<GeometryPoint> GetPossibleCorners() {
            var possibleCorners = new List<GeometryPoint>();

            var houghTransform = new HoughTransform(this);
            // houghTransform.GetHoughArrayImage();

            houghTransform.GetLines();
            
            return possibleCorners;
        }
    }
}