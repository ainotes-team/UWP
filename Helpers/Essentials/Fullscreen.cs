using Windows.UI.ViewManagement;

namespace Helpers.Essentials {
    public static class Fullscreen {
        public static bool IsFullscreen() => ApplicationView.GetForCurrentView().IsFullScreenMode;
        
        public static void Enable() {
            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
        }

        public static void Disable() {
            ApplicationView.GetForCurrentView().ExitFullScreenMode();
        }

        public static void Toggle() {
            if (ApplicationView.GetForCurrentView().IsFullScreenMode) {
                Disable();
            } else {
                Enable();
            }
        }
    }
}