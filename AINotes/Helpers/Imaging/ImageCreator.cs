using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using Helpers;

namespace AINotes.Helpers.Imaging {
    public static class ImageCreator {
        public static async Task<Stream> CreateImageFromView(UIElement nativeElement) {
            var resultStream = new InMemoryRandomAccessStream();

            var renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(nativeElement);
            var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, resultStream);
            
            encoder.SetPixelData(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Ignore,
                (uint)renderTargetBitmap.PixelWidth,
                (uint)renderTargetBitmap.PixelHeight,
                DisplayInformation.GetForCurrentView().LogicalDpi,
                DisplayInformation.GetForCurrentView().LogicalDpi,
                pixelBuffer.ToArray()
            );
            
            await encoder.FlushAsync();

            return resultStream.AsStream();
        }

        public static async Task<Stream> CreateImageFromWindow(int width = 0, int height = 0) {
            Logger.Log("[ImageCreatorUwp]", "-> ImageFromWindow", logLevel: LogLevel.Verbose);

            var resultStream = new InMemoryRandomAccessStream();
            var renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(null, width, height);

            var pixelBuffer = (await renderTargetBitmap.GetPixelsAsync()).ToArray();

            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, resultStream);

            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint) renderTargetBitmap.PixelWidth, (uint) renderTargetBitmap.PixelHeight, DisplayInformation.GetForCurrentView().LogicalDpi, DisplayInformation.GetForCurrentView().LogicalDpi, pixelBuffer);

            await encoder.FlushAsync();

            var result = resultStream.AsStream();

            Logger.Log("[ImageCreatorUwp]", "<- ImageFromWindow", logLevel: LogLevel.Verbose);
            return result;
        }
    }
}