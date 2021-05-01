using System;
using Windows.UI.Xaml;
using AINotes.Controls;
using Microsoft.Toolkit.Uwp.UI.Animations;

namespace AINotes.Screens {
    public partial class CameraScreen {
        public CameraScreen() {
            InitializeComponent();
            RegisterShortcuts();
        }

        public override async void OnLoad() {
            base.OnLoad();
            LoadToolbar();

            // check camera
            if (!await HasSystemCamera()) {
                // show error popup & go back to the editor screen
                ShowNoCameraPopup();
                return;
            }

            // reset
            ResetButtons();
            ResetImages();

            // start preview
            await CameraPreview.StartPreviewAsync(CameraPanel.Back);
        }

        public override async void OnUnload() {
            base.OnUnload();
            
            // cleanup
            await CameraPreview.CleanupCameraAsync();
        }

        private void OnCaptureButtonClicked(object sender, EventArgs e) {
            CaptureImage();
        }
        
        private void OnSwitchCameraButtonClicked(object sender, EventArgs e) {
            SwitchCamera();
        }
        
        private async void OnOptionsButtonClicked(object sender, EventArgs e) {
            switch (OptionsPanel.Visibility) {
                case Visibility.Visible:
                    OptionsPanel.Visibility = Visibility.Collapsed;
                    await ((UIElement) sender).Rotate(value: 0.0f, centerX: 24.0f, centerY: 24.0f, duration: 250, delay: 0, easingType: EasingType.Default).StartAsync();
                    break;
                case Visibility.Collapsed:
                    OptionsPanel.Visibility = Visibility.Visible;
                    await ((UIElement) sender).Rotate(value: 180.0f, centerX: 24.0f, centerY: 24.0f, duration: 250, delay: 0, easingType: EasingType.Default).StartAsync();
                    break;
            }
        }

        private void OnAutoScanButtonClicked(object sender, EventArgs e) {
            ToggleAutoScan();
        }
        
        private void OnEditImagesButtonClicked(object sender, EventArgs e) {
            OpenImageEditor();
        }
        
        private async void OnInsertImagesButtonClicked(object sender, EventArgs e) {
            await InsertImages();
        }
        
        private void OnPhotoCaptured(Uri uri) {
            AddCapturedImage(uri);
            EnableButtons();
        }
    }
}