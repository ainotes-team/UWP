using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using MaterialComponents;

namespace AINotes.Helpers.Extensions {
    public static class ImageSourceExtensions {
        public static void SetProfilePicture(this MDToolbarItem toolbarItem, string userId) {
            var profilePictureUrl = Configuration.DefaultProfilePicture;

            if (!string.IsNullOrEmpty(CloudAdapter.CurrentRemoteUserModel.ProfilePicture)) {
                var serverUrl = Preferences.ServerUrl.ToString().EndsWith("/") ? Preferences.ServerUrl : Preferences.ServerUrl + "/";
                profilePictureUrl = serverUrl + $"images/profilepicture/{userId}";
            }
            
            toolbarItem.ImageSource = new BitmapImage(new Uri(profilePictureUrl)) {
                CreateOptions = BitmapCreateOptions.IgnoreImageCache
            };
        }
        
        public static void SetProfilePicture(this Image image, string userId) {
            var profilePictureUrl = Configuration.DefaultProfilePicture;

            if (!string.IsNullOrEmpty(CloudAdapter.CurrentRemoteUserModel.ProfilePicture)) {
                var serverUrl = Preferences.ServerUrl.ToString().EndsWith("/") ? Preferences.ServerUrl : Preferences.ServerUrl + "/";
                profilePictureUrl = serverUrl + $"images/profilepicture/{userId}";
            }
            
            image.Source = new BitmapImage(new Uri(profilePictureUrl)) {
                CreateOptions = BitmapCreateOptions.IgnoreImageCache
            };
        }
    }
}