using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Helpers;
using Helpers.Extensions;
using Imaging.Library.Entities;

namespace AINotes.Helpers.Imaging {
    public static class ImageEditingHelper {
        public static async Task<byte[]> Rotate(byte[] encodedBytes, int degrees) {
            var sources = new PixelMap[2];
            sources = await GetPixelMap(encodedBytes, sources);
            var pixelMap = sources[0];
            
            var resultStream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, resultStream);

            while (degrees < 0) degrees += 360;
            switch (degrees) {
                case 0:
                    encoder.BitmapTransform.Rotation = BitmapRotation.None;
                    break;
                case 90:
                    encoder.BitmapTransform.Rotation = BitmapRotation.Clockwise90Degrees;
                    break;
                case 180:
                    encoder.BitmapTransform.Rotation = BitmapRotation.Clockwise180Degrees;
                    break;
                case 270:
                    encoder.BitmapTransform.Rotation = BitmapRotation.Clockwise270Degrees;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(degrees), @"currently only multiples of 90° are supported");
            }
            
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint) pixelMap.Width, (uint) pixelMap.Height, pixelMap.DpiX, pixelMap.DpiY, pixelMap.ToByteArray());

            await encoder.FlushAsync();
            return resultStream.AsStream().ReadAllBytes();
        }

        public static async Task<PixelMap[]> GetPixelMap(byte[] encodedBytes, PixelMap[] sources) {
            var wb = await BitmapDecoder.CreateAsync(new MemoryStream(encodedBytes).AsRandomAccessStream());

            var width = wb.PixelWidth;
            var height = wb.PixelHeight;
            var dpiX = wb.DpiX;
            var dpiY = wb.DpiY;

            for (var i = 0; i < sources.Length; i++) sources[i] = new PixelMap((int) width, (int) height, (int) dpiX, (int) dpiY);

            // var stride = wb.PixelWidth * ((bytesPerPixel + 7) / 8);

            var data = (await wb.GetPixelDataAsync()).DetachPixelData();

            for (var y = 0; y < height; y++) {
                for (var x = 0; x < width; x++) {
                    foreach (var source in sources)
                        source[x, y] = new Pixel {
                            R = data[(y * width + x) * 4 + 0],
                            G = data[(y * width + x) * 4 + 1],
                            B = data[(y * width + x) * 4 + 2],
                            A = data[(y * width + x) * 4 + 3]
                        };
                }
            }

            return sources;
        }

        public static async Task SavePixelMapToFile(PixelMap pixelMap) {
            var resultStream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, resultStream);

            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint) pixelMap.Width, (uint) pixelMap.Height, pixelMap.DpiX, pixelMap.DpiY, pixelMap.ToByteArray());

            await encoder.FlushAsync();

            var resultBytes = resultStream.AsStream().ReadAllBytes();
            await LocalFileHelper.WriteFileAsync("auto_cropped.png", resultBytes);
        }

        public static async Task<Stream> SavePixelMapToStream(PixelMap pixelMap) {
            var resultStream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, resultStream);

            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint) pixelMap.Width, (uint) pixelMap.Height, pixelMap.DpiX, pixelMap.DpiY, pixelMap.ToByteArray());

            await encoder.FlushAsync();
            return resultStream.AsStream();
        }
    }
}