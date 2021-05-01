using System;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace AINotes.Controls {
    public partial class CustomCameraPreview {
        
        public event Action<Uri> OnPhotoCaptured;

        public async void Capture() {
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync("camera_pic.jpg", CreationCollisionOption.GenerateUniqueName);
            await _mediaCapture.CapturePhotoToStorageFileAsync(ImageEncodingProperties.CreateJpeg(), file);
            OnPhotoCaptured?.Invoke( new Uri(file.Path));
        } 
    }
}