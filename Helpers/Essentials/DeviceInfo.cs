using System;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.System.Profile;
using Windows.UI.ViewManagement;

namespace Helpers.Essentials {
    public readonly struct DeviceIdiom : IEquatable<DeviceIdiom> {
        private readonly string _deviceIdiom;

        public static DeviceIdiom Phone { get; } = new DeviceIdiom(nameof(Phone));

        public static DeviceIdiom Tablet { get; } = new DeviceIdiom(nameof(Tablet));

        public static DeviceIdiom Desktop { get; } = new DeviceIdiom(nameof(Desktop));

        // ReSharper disable once InconsistentNaming
        public static DeviceIdiom TV { get; } = new DeviceIdiom(nameof(TV));

        public static DeviceIdiom Unknown { get; } = new DeviceIdiom(nameof(Unknown));

        private DeviceIdiom(string deviceIdiom) {
            if (deviceIdiom == null) throw new ArgumentNullException(nameof(deviceIdiom));

            if (deviceIdiom.Length == 0) throw new ArgumentException(nameof(deviceIdiom));

            _deviceIdiom = deviceIdiom;
        }
        
        public bool Equals(DeviceIdiom other) => Equals(other._deviceIdiom);

        internal bool Equals(string other) => string.Equals(_deviceIdiom, other, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is DeviceIdiom && Equals((DeviceIdiom) obj);

        public override int GetHashCode() => _deviceIdiom == null ? 0 : _deviceIdiom.GetHashCode();

        public override string ToString() => _deviceIdiom ?? string.Empty;

        public static bool operator ==(DeviceIdiom left, DeviceIdiom right) => left.Equals(right);

        public static bool operator !=(DeviceIdiom left, DeviceIdiom right) => !left.Equals(right);
    }

    public enum DeviceType {
        Physical = 1,
        Virtual = 2
    }

    public static class DeviceInfo {
        private static readonly EasClientDeviceInformation EasDeviceInfo;
        private static DeviceIdiom _currentIdiom;

        static DeviceInfo() {
            EasDeviceInfo = new EasClientDeviceInformation();
            _currentIdiom = DeviceIdiom.Unknown;
        }

        public static string Manufacturer => EasDeviceInfo.SystemManufacturer;

        public static string Name => EasDeviceInfo.FriendlyName;

        public static string VersionString {
            get {
                var version = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
                if (!ulong.TryParse(version, out var v)) return version;
                var v1 = (v & 0xFFFF000000000000L) >> 48;
                var v2 = (v & 0x0000FFFF00000000L) >> 32;
                var v3 = (v & 0x00000000FFFF0000L) >> 16;
                var v4 = v & 0x000000000000FFFFL;
                return $"{v1}.{v2}.{v3}.{v4}";
            }
        }

        public static DeviceIdiom Idiom {
            get {
                switch (AnalyticsInfo.VersionInfo.DeviceFamily) {
                    case "Windows.Mobile":
                        _currentIdiom = DeviceIdiom.Phone;
                        break;
                    case "Windows.Universal":
                    case "Windows.Desktop": {
                        try {
                            var uiMode = UIViewSettings.GetForCurrentView().UserInteractionMode;
                            _currentIdiom = uiMode == UserInteractionMode.Mouse ? DeviceIdiom.Desktop : DeviceIdiom.Tablet;
                        } catch (Exception ex) {
                            Logger.Log($"Unable to get device: {ex.Message}");
                        }
                    }
                        break;
                    case "Windows.Xbox":
                    case "Windows.Team":
                        _currentIdiom = DeviceIdiom.TV;
                        break;
                    default:
                        _currentIdiom = DeviceIdiom.Unknown;
                        break;
                }

                return _currentIdiom;
            }
        }

        public static DeviceType DeviceType {
            get {
                var isVirtual = EasDeviceInfo.SystemProductName.Contains("Virtual") || EasDeviceInfo.SystemProductName == "HMV domU";

                return isVirtual ? DeviceType.Virtual : DeviceType.Physical;
            }
        }

        public static string Platform => "UWP";
    }
}