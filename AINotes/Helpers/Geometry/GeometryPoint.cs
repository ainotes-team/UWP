using System.Drawing;
using Point = Windows.Foundation.Point;

namespace AINotes.Helpers.Geometry {
    public class GeometryPoint {
        public double X;
        public double Y;

        public GeometryPoint() {
            X = double.NaN;
            Y = double.NaN;
        }

        public GeometryPoint(double x, double y) {
            X = x;
            Y = y;
        }

        public (double, double) GetPosition() => (X, Y);
            
        public void AlignToGrid() {
            var horizontal = App.EditorScreen.BackgroundHorizontalStep;
            var vertical = App.EditorScreen.BackgroundVerticalStep;

            var xMod = X % horizontal;
            var yMod = Y % vertical;

            X = xMod <= horizontal / 2 ? X - xMod : X - xMod + horizontal;
            Y = yMod <= vertical / 2 ? Y - yMod : Y - yMod + vertical;
        }

        public PointF ToPointF() {
            return new PointF((float) X, (float) Y);
        }
        public static GeometryPoint FromPoint(Point p) {
            return new GeometryPoint(p.X, p.Y);
        }

        public void Deconstruct(out double x, out double y) {
            x = X;
            y = Y;
        }

        public Point ToPoint() {
            return new Point(X, Y);
        }
    };
}