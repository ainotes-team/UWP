using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace MaterialComponents {
    public class AccountItem {
        public string UserId { get; set; }
        public string Username { get; }
        public string Email { get; }
        public ImageSource ProfilePicture { get; }
        public FrameworkElement ExtraOptionsView { get; set; }

        public readonly string Token;

        public AccountItem(string username, string email, string profilePictureUrl, string token) {
            Username = username;
            Email = email;
            if (profilePictureUrl != null) {
                ProfilePicture = new BitmapImage(new Uri(profilePictureUrl)) { CreateOptions = BitmapCreateOptions.IgnoreImageCache };
            }
            Token = token;
        }
    }

    public partial class MDAccountsList {
        public event Action<AccountItem> OnItemSelected;

        public MDAccountsList() {
            InitializeComponent();
            AccountList.ItemClick += (sender, args) => OnItemSelected?.Invoke((AccountItem) args.ClickedItem);
        }

        public void SetItems(IEnumerable<AccountItem> accountItems) {
            AccountList.ItemsSource = accountItems.ToList();
        }

        private void ItemPointerEntered(object sender, PointerRoutedEventArgs e) {
            ((Frame) sender).Background = new SolidColorBrush(Colors.LightGray);
        }

        private void ItemPointerExited(object sender, PointerRoutedEventArgs e) {
            ((Frame) sender).Background = new SolidColorBrush(Colors.Transparent);
        }
    }
}