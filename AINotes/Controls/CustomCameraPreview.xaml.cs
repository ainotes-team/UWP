using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.System.Display;
using Windows.UI.Core;
using AINotes.Helpers;
using Helpers;
using Helpers.Essentials;
using MaterialComponents;

namespace AINotes.Controls {
    public enum CameraPanel {
        Front,
        Back
    }

    public partial class CustomCameraPreview {
        private MediaCapture _mediaCapture;
        private bool _isPreviewing;

        private readonly DisplayRequest _displayRequest = new DisplayRequest();

        private CameraPanel _cameraPanel;

        public CustomCameraPreview() {
            InitializeComponent();
        }

        public async Task StartPreviewAsync(CameraPanel panel) {
            _cameraPanel = panel;
            try {
                DeviceInformation cameraDevice = null;
                switch (panel) {
                    case CameraPanel.Back:
                        cameraDevice = await FindCameraDeviceByPanelAsync(Panel.Back);
                        break;
                    case CameraPanel.Front:
                        cameraDevice = await FindCameraDeviceByPanelAsync(Panel.Front);
                        break;
                }

                _mediaCapture = new MediaCapture();
                if (cameraDevice != null) {
                    var settings = new MediaCaptureInitializationSettings {VideoDeviceId = cameraDevice.Id};
                    await _mediaCapture.InitializeAsync(settings);
                } else {
                    await _mediaCapture.InitializeAsync();
                }

                _displayRequest.RequestActive();
                PreviewControl.Source = _mediaCapture;
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            } catch (UnauthorizedAccessException ex) {
                App.Page.Load(App.EditorScreen);
                Logger.Log("[CustomCameraPreview]", "StartPreviewAsync: Access to the camera is not allowed.", logLevel: LogLevel.Warning);
                App.Page.Notifications.Add(new MDNotification($"Error:\nCannot access the camera. (Unauthorized)"));
                SentryHelper.CaptureCaughtException(ex);
                return;
            } catch (Exception ex) {
                Logger.Log("[CustomCameraPreview]", $"StartPreviewAsync: Accessing the camera failed:", ex, logLevel: LogLevel.Error);
                App.Page.Load(App.EditorScreen);
                App.Page.Notifications.Add(new MDNotification($"Error:\nCannot access the camera."));
                SentryHelper.CaptureCaughtException(ex);
                return;
            }

            try {
                await _mediaCapture.StartPreviewAsync();
                _isPreviewing = true;
            }
            catch (System.IO.FileLoadException) {
                _mediaCapture.CaptureDeviceExclusiveControlStatusChanged += CaptureDeviceExclusiveControlStatusChanged;
            }
        }

        private async void CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args) {
            switch (args.Status) {
                case MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable:
                    // camera is currently used by an other app
                    App.Page.Load(App.EditorScreen);
                    break;
                case MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable when !_isPreviewing:
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => {
                        await StartPreviewAsync(CameraPanel.Back);
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task CleanupCameraAsync() {
            if (_mediaCapture != null) {
                try {
                    if (_isPreviewing) {
                        await _mediaCapture.StopPreviewAsync();
                    }

                    await MainThread.InvokeOnMainThreadAsync(() => {
                        PreviewControl.Source = null;
                        _displayRequest?.RequestRelease();

                        _mediaCapture.Dispose();
                        _mediaCapture = null;
                    });
                } catch (Exception ex) {
                    Logger.Log("[CustomCameraPreview]", "CleanupCameraAsync", ex.ToString(), logLevel: LogLevel.Error);
                }
            }
        }
        
        private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Panel desiredPanel) {
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            var desiredDevice = allVideoDevices.FirstOrDefault(d => d.EnclosureLocation != null && d.EnclosureLocation.Panel == desiredPanel);
            return desiredDevice ?? allVideoDevices.FirstOrDefault();
        }

        public async void Switch() {
            switch (_cameraPanel) {
                case CameraPanel.Front:
                    await StartPreviewAsync(CameraPanel.Back);
                    break;
                case CameraPanel.Back:
                    await StartPreviewAsync(CameraPanel.Front);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}