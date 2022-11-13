using System;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Helpers;
using MaterialComponents;

namespace AINotes.Controls {
    public partial class CustomCameraPreview {
        
        public event Action<Uri> OnPhotoCaptured;

        public async void Capture() {
            try {
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync("camera_pic.jpg", CreationCollisionOption.GenerateUniqueName);
                await _mediaCapture?.CapturePhotoToStorageFileAsync(ImageEncodingProperties.CreateJpeg(), file);
                OnPhotoCaptured?.Invoke( new Uri(file.Path));
            } catch (Exception ex) {
                Logger.Log("[CustomCameraPreview]", "StartPreviewAsync: Capture failed:", ex, logLevel: LogLevel.Error);
                App.Page.Load(App.EditorScreen);
                App.Page.Notifications.Add(new MDNotification("Error:\nCapture failed. :("));
            }
        } 
    }
}