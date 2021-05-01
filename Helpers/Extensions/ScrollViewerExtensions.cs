using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Helpers.Extensions {
    public static class ScrollViewerExtensions {
        public static void SetDisableScrolling(this ScrollViewer scrollViewer, bool disable) {
            scrollViewer.ManipulationMode = disable ? ManipulationModes.None : ManipulationModes.System;
            scrollViewer.ZoomMode = disable ? ZoomMode.Disabled : ZoomMode.Enabled;
            scrollViewer.HorizontalScrollMode = scrollViewer.VerticalScrollMode = disable ? ScrollMode.Disabled : ScrollMode.Enabled;
        }

        public static void ScrollToElement(this ScrollViewer scrollViewer, UIElement element, bool isVerticalScrolling = true, bool smoothScrolling = true, float? zoomFactor = null) {
            var transform = element.TransformToVisual((UIElement) scrollViewer.Content);
            var position = transform.TransformPoint(new Point(0, 0));

            if (isVerticalScrolling) {
                scrollViewer.ChangeView(null, position.Y, zoomFactor, !smoothScrolling);
            } else {
                scrollViewer.ChangeView(position.X, null, zoomFactor, !smoothScrolling);
            }
        }
    }
}