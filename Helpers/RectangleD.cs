using Windows.Foundation;

namespace Helpers {
    public struct RectangleD {
        public double X { get; }
        public double Y { get; }
        public double Width { get; }
        public double Height { get; }
        
        public Point Position => new Point(X, Y);
        public Size Size => new Size(Width, Height);

        public RectangleD(double x, double y, double width, double height) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
        
        public RectangleD(Point p, double width, double height) {
            X = p.X;
            Y = p.Y;
            Width = width;
            Height = height;
        }

        public RectangleD(Point p, Size s) {
            X = p.X;
            Y = p.Y;
            Width = s.Width;
            Height = s.Height;
        }

        public void Deconstruct(out double x, out double y, out double width, out double height) {
            x = X;
            y = Y;
            width = Width;
            height = Height;
        }

        public override string ToString() => $"RectangleD X: {X} | Y: {Y} | W: {Width} | H: {Height}";

        public RectangleD Clone() => new RectangleD(X, Y, Width, Height);
    }
}