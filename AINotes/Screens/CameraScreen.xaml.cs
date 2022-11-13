using System;
using Windows.UI.Xaml;
using AINotes.Controls;

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
        
        private void OnOptionsButtonClicked(object sender, EventArgs e) {
            var senderElement = (UIElement) sender;
            switch (OptionsPanel.Visibility) {
                case Visibility.Visible:
                    OptionsPanel.Visibility = Visibility.Collapsed;
                    senderElement.Rotation = 0.0f;
                    break;
                case Visibility.Collapsed:
                    OptionsPanel.Visibility = Visibility.Visible;
                    senderElement.Rotation = 180.0f;
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