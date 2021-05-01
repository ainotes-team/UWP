using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml.Controls;
using AINotes.Components.Implementations;
using AINotes.Components.Tools;
using AINotes.Controls.ImageEditing;
using AINotes.Controls.Popups;
using Helpers;
using Helpers.Essentials;
using MaterialComponents;

namespace AINotes.Screens {
    public partial class CameraScreen {
        // state
        private bool AutoScan { get; set; }
        private readonly List<AnalyzedImage> _analyzedImages = new List<AnalyzedImage>();

        private void LoadToolbar() {
            App.Page.OnBackPressed = () => App.Page.Load(App.EditorScreen);
            App.Page.PrimaryToolbarChildren.Clear();
        }

        private void CaptureImage() {
            CaptureButton.IsEnabled = false;
            CameraPreview.Capture();
        }

        private void OpenImageEditor() {
            if (_analyzedImages.Count == 0) return;
            PreviewsStack.Children.Clear();

            App.ImageEditorScreen.LoadWithCallback(_analyzedImages, OnImageEditorScreenFinished);
        }

        private async void OnImageEditorScreenFinished(List<AnalyzedImage> list) {
            App.Page.Load(App.EditorScreen);
            var imageComponents = new List<ImageComponent>();
            foreach (var image in list) {
                imageComponents.Add(await image.ToComponent());
            }

            ImageComponentTool.AddDocumentComponents(imageComponents);
        }

        private void AddCapturedImage(Uri uri) {
            var absolutePath = uri.LocalPath;
            
            // check duplicates (?)
            if (_analyzedImages.Any(image => image.AbsolutePath == absolutePath)) return;
            
            // read & add
            var imageBytes = File.ReadAllBytes(absolutePath);
            var analyzedImage = new AnalyzedImage(imageBytes, absolutePath, setCropped: AutoScan);
            PreviewsStack.Children.Add(analyzedImage);
            _analyzedImages.Add(analyzedImage);
        }

        private async Task InsertImages() {
            App.Page.Load(App.EditorScreen);
            var imageComponents = new List<ImageComponent>();
            foreach (var image in _analyzedImages) {
                imageComponents.Add(await image.ToComponent());
            }

            ImageComponentTool.AddDocumentComponents(imageComponents);
        }

        private async Task<bool> HasSystemCamera() {
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            // Logger.Log($"[{nameof(CameraScreen)}]", $"{(await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture)).Count} video devices found");
            // Logger.Log($"[{nameof(CameraScreen)}]", $"{(await DeviceInformation.FindAllAsync(DeviceClass.Location)).Count} location devices found");
            // Logger.Log($"[{nameof(CameraScreen)}]", $"{(await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture)).Count} audio capture devices found");
            // Logger.Log($"[{nameof(CameraScreen)}]", $"{(await DeviceInformation.FindAllAsync(DeviceClass.ImageScanner)).Count} image scanner devices found");
            // Logger.Log($"[{nameof(CameraScreen)}]", $"{(await DeviceInformation.FindAllAsync(DeviceClass.AudioRender)).Count} audio render devices found");
            return devices.Count > 0;
        }
        
        private void ShowNoCameraPopup() {
            MainThread.BeginInvokeOnMainThread(() => {
                new MDContentPopup(ResourceHelper.GetString("error"), new StackPanel {
                    Children = {
                        new MDLabel(ResourceHelper.GetString("Popup.NoCamera"))
                    }
                }, () => {
                    App.Page.Load(App.EditorScreen);
                }, cancelable: false, closeOnOk: true).Show();
            });
        }

        private void SwitchCamera() {
            CameraPreview.Switch();
        }

        private void ResetButtons() {
            if (EditImagesButton != null) EditImagesButton.IsEnabled = false;
            if (InsertImagesButton != null) InsertImagesButton.IsEnabled = false;
        }

        private void EnableButtons() {
            CaptureButton.IsEnabled = EditImagesButton.IsEnabled = InsertImagesButton.IsEnabled = true;
        }

        private void ResetImages() {
            _analyzedImages.Clear();
            PreviewsStack?.Children.Clear();
        }

        private void ToggleAutoScan() {
            AutoScan = AutoScanButton.IsSelected;
        }
    }
}