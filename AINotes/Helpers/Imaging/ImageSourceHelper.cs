using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using AINotes.Controls.ImageEditing;

namespace AINotes.Helpers.Imaging {
    public static class ImageSourceHelper {
        public static ImageSource FromName(string icon) {
            return new BitmapImage {
                UriSource = new Uri(icon),
                CreateOptions = BitmapCreateOptions.IgnoreImageCache
            };
        }

        public static async Task<BitmapImage> AsBitmapImage(byte[] byteArray, AnalyzedImage analyzedImage = null) {
            var bitmapSource = new BitmapImage();

            bitmapSource.ImageOpened += (sender, _) => {
                if (analyzedImage == null) return;
                if (!(sender is BitmapImage bm)) return;
                var width = bm.PixelWidth;
                var height = bm.PixelHeight;
                
                analyzedImage.OriginalSize = new Size(width, height);
            };
            
            using (var stream = new InMemoryRandomAccessStream()) {
                await stream.WriteAsync(byteArray.AsBuffer());
                stream.Seek(0);
                await bitmapSource.SetSourceAsync(stream);
            }

            return bitmapSource;
        }
    }
}