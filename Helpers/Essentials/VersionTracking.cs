using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Helpers.Extensions;
using HtmlAgilityPack;

namespace Helpers.Essentials {
    public static class VersionTracking {
        public static bool IsFirstLaunchEver { get; }
        
        public static bool IsFirstLaunchForCurrentVersion { get; }
        public static bool IsFirstLaunchForCurrentBuild { get; }

        public static async Task<bool> GetUpdatesAvailable() {
            try {
                // request store description containing the current store version string
                var storeVersionString = await Task.Run(() => {
                    try {
                        var storePage = new HtmlWeb().Load("https://www.microsoft.com/de-de/p/ainotes/9ngx8jnlllcj/");
                        var changelogText = storePage.DocumentNode.SelectSingleNode("//*[@id='product-description']")?.InnerHtml;

                        if (changelogText == null) return null;

                        var versionIdx = changelogText.LastIndexOf("Version", StringComparison.Ordinal);
                        var changeString = changelogText.Substring(versionIdx, changelogText.Length - versionIdx);
                        return changeString.Substring(0, changeString.IndexOf("(", StringComparison.InvariantCulture)).Replace(new [] {
                            "\n", " ", "Version"
                        }, "");
                    } catch (Exception) {
                        return null;
                    }
                });

                if (storeVersionString == null) return false;

                Logger.Log("[VersionTracking]", "GetUpdatesAvailable: StoreVersionString", storeVersionString);

                // parse store version
                var storeVersion = new PackageVersion {
                    Major = ushort.Parse(storeVersionString.Split(".")[0]),
                    Minor = ushort.Parse(storeVersionString.Split(".")[1]),
                    Build = ushort.Parse(storeVersionString.Split(".")[2]),
                    Revision = 0, // revision is currently unused
                };
                
                // get local version
                var version = Package.Current.Id.Version;

                // check majors
                if (storeVersion.Major > version.Major) return true;
                if (storeVersion.Major < version.Major) return false;
                
                // check minors
                if (storeVersion.Minor > version.Minor) return true;
                if (storeVersion.Minor < version.Minor) return false;
                
                // check builds
                if (storeVersion.Build > version.Build) return true;
                if (storeVersion.Build < version.Build) return false;
                
                // check revisions
                if (storeVersion.Revision > version.Revision) return true;
                if (storeVersion.Revision < version.Revision) return false;

            } catch (Exception ex) {
                Logger.Log("[VersionTracking]", "Error while checking for updates:", ex.ToString(), logLevel: LogLevel.Error);
            }
            
            return false;
        }


        private const string VersionsKey = "VersionTracking.Versions";
        private const string BuildsKey = "VersionTracking.Builds";

        private const string SharedName = "ainotes.versiontracking";
        static VersionTracking() {
            // actual version history
            Dictionary<string, List<string>> versionTrail;
            IsFirstLaunchEver = !AppPreferencesHelper.ContainsKey(VersionsKey, SharedName) || !AppPreferencesHelper.ContainsKey(BuildsKey, SharedName);
            if (IsFirstLaunchEver) {
                versionTrail = new Dictionary<string, List<string>> {
                    {VersionsKey, new List<string>()},
                    {BuildsKey, new List<string>()}
                };
            } else {
                versionTrail = new Dictionary<string, List<string>> {
                    {VersionsKey, ReadHistory(VersionsKey).ToList()},
                    {BuildsKey, ReadHistory(BuildsKey).ToList()}
                };
            }

            IsFirstLaunchForCurrentVersion = !versionTrail[VersionsKey].Contains(AppInfo.VersionString);
            if (IsFirstLaunchForCurrentVersion) {
                versionTrail[VersionsKey].Add(AppInfo.VersionString);
            }

            IsFirstLaunchForCurrentBuild = !versionTrail[BuildsKey].Contains(AppInfo.BuildString);
            if (IsFirstLaunchForCurrentBuild) {
                versionTrail[BuildsKey].Add(AppInfo.BuildString);
            }

            if (!IsFirstLaunchForCurrentVersion && !IsFirstLaunchForCurrentBuild) return;
            WriteHistory(VersionsKey, versionTrail[VersionsKey]);
            WriteHistory(BuildsKey, versionTrail[BuildsKey]);
        }

        private static IEnumerable<string> ReadHistory(string key) => AppPreferencesHelper.Get(key, null, SharedName)?.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
        private static void WriteHistory(string key, IEnumerable<string> history) => AppPreferencesHelper.Set(key, string.Join("|", history), SharedName);
    }
}