using System.Globalization;
using Windows.ApplicationModel;

namespace Helpers.Essentials {
    public static class AppInfo {
        public static string VersionString {
            get {
                var version = Package.Current.Id.Version;
                return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            }
        }

        public static string BuildString => Package.Current.Id.Version.Build.ToString(CultureInfo.InvariantCulture);
    }
}