using Windows.UI.Xaml.Controls;
using AINotes.Controls.Pages;

namespace AINotes.Controls.Popups {
    public static class PopupNavigation {
        public static ContentDialog CurrentPopup;
        public static void CloseCurrentPopup() {
            if (CurrentPopup is MDPopup c) {
                c.Hide();
            } else {
                CurrentPopup?.Hide();
            }

            CurrentPopup = null;
        }

        public static void OpenPopup(ContentDialog popup) {
            CustomDropdown.CloseDropdown();

            CurrentPopup = popup;
            if (CurrentPopup is MDPopup c) {
                c.Show();
            } else {
                CurrentPopup.ShowAsync();
            }
        }

        public static void ShowError(string message) {
            if (CurrentPopup is MDContentPopup popup) {
                popup.ShowErrorMessage(message);
            }
        }
    }
}