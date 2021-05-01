using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AINotes.Controls.Containers;
using Helpers;

namespace AINotes.Helpers.Extensions {
    public static class CanvasExtensions {
        public static void AddChild(this Canvas canvas, UIElement child, Point p) {
            if (canvas is DocumentCanvas dc) {
                AddChild(dc, child, p);
                return;
            }
            canvas.Children.Add(child);
            Canvas.SetLeft(child, p.X);
            Canvas.SetTop(child, p.Y);
        }
        
        public static void AddChild(this DocumentCanvas canvas, UIElement child, Point p) {
            canvas.Children.Add(child);
            Canvas.SetLeft(child, p.X);
            Canvas.SetTop(child, p.Y);
        }
        
        public static void AddChild(this Canvas canvas, FrameworkElement child, RectangleD r) {
            if (canvas is DocumentCanvas dc) {
                AddChild(dc, child, r);
                return;
            }
            canvas.Children.Add(child);
            
            Canvas.SetLeft(child, r.X);
            Canvas.SetTop(child, r.Y);

            child.Width = r.Width;
            child.Height = r.Height;
        }
        
        public static void AddChild(this DocumentCanvas canvas, FrameworkElement child, RectangleD r) {
            canvas.Children.Add(child);
            
            Canvas.SetLeft(child, r.X);
            Canvas.SetTop(child, r.Y);

            child.Width = r.Width;
            child.Height = r.Height;
        }
    }
}