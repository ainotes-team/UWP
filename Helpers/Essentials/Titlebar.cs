using Windows.UI;
using Windows.UI.ViewManagement;

namespace Helpers.Essentials {
    public static class Titlebar {
        public static void SetColor(Color c) {
            var applicationTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            applicationTitleBar.BackgroundColor = applicationTitleBar.ButtonBackgroundColor = c;
        }
    }
}