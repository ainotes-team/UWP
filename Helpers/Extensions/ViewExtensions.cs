using Windows.Foundation;
using Windows.UI.Xaml;

namespace Helpers.Extensions {
    public static class ViewExtensions {
        // Absolute Coordinates
        public static Point GetAbsoluteCoordinates(this UIElement uiElement) {
            var ttv = uiElement.TransformToVisual(Window.Current.Content);
            var screenCoords = ttv.TransformPoint(new Point(0, 0));
            return screenCoords;
        }

        // Hit Test
        public static bool HitTest(this UIElement self, Point p) {
            var (vW, vH) = (self.RenderSize.Width, self.RenderSize.Height);

            var vPoint = self.GetAbsoluteCoordinates();
            var (vX, vY) = (vPoint.X, vPoint.Y);

            var (pX, pY) = (p.X, p.Y);

            return !(pX < vX) && !(pY < vY) && !(pX > vX + vW) && !(pY > vY + vH);
        }
    }
}