using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Helpers {
    public static class CanvasHelper {
        public static void SetPosition(FrameworkElement element, Windows.Foundation.Point p) {
            Canvas.SetLeft(element, p.X);
            Canvas.SetTop(element, p.Y);
        }
        
        public static void SetSize(FrameworkElement element, Windows.Foundation.Size s) {
            element.Width = s.Width;
            element.Height = s.Height;
        }
    }
}