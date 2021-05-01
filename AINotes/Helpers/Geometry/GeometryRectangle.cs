using System;
using System.Drawing;

namespace AINotes.Helpers.Geometry {
    public class GeometryRectangle {
        public double X;
        public double Y;

        public double Width;
        public double Height;

        public GeometryRectangle() {
            X = double.NaN;
            Y = double.NaN;

            Width = double.NaN;
            Height = double.NaN;
        }

        public GeometryRectangle(double x, double y, double width, double height) {
            X = x;
            Y = y;

            Width = width;
            Height = height;
        }

        public GeometryRectangle(Rectangle rectangle) {
            X = rectangle.X;
            Y = rectangle.Y;

            Width = rectangle.Width;
            Height = rectangle.Height;
        }

        public GeometryRectangle(RectangleF rectangleF) {
            X = rectangleF.X;
            Y = rectangleF.Y;

            Width = rectangleF.Width;
            Height = rectangleF.Height;
        }

        public (double, double, double, double) GetDimensions() {
            return (X, Y, Width, Height);
        }

        public RectangleF ToRectangleF() {
            return new RectangleF((float) X, (float) Y, (float) Width, (float) Height);
        }

        public bool IntersectsWith(GeometryRectangle rectangle) {
            return ToRectangleF().IntersectsWith(rectangle.ToRectangleF());
        }

        public bool Contains(GeometryPoint p0) {
            return ToRectangleF().Contains(p0.ToPointF());
        }

        public GeometryRectangle AddMargin(double margin) {
            return new GeometryRectangle(X - margin, Y - margin, Width + margin, Height + margin);
        }

        public double GetLongSide() => Math.Max(Width, Height);

        public double GetShortSide() => Math.Min(Width, Height);
    }
}