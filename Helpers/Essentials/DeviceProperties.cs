using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Input;
using Windows.Graphics.Display;

namespace Helpers.Essentials {
    public static class DeviceProperties {
        public static bool GetTouchCapable() {
            var touchCapabilities = new TouchCapabilities();
            return touchCapabilities.TouchPresent != 0;
        }

        public static async Task<int> GetCameraCount() {
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            return allVideoDevices.Count;
        }
        
        public static double DisplayDensity => DisplayInformation.GetForCurrentView().LogicalDpi / 96.0;
    }
}