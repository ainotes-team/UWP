namespace Helpers.Extensions {
    public static class PointExtensions {
        public static void Deconstruct(this Windows.Foundation.Point p, out double x, out double y) {
            x = p.X;
            y = p.Y;
        }
    }
}