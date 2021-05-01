using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Helpers.Essentials {
    public static class PointerHelper {
        public static void SetPointerCursor(CoreCursorType pointerCursor) {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(pointerCursor, 1);
        }
    }
}