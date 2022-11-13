using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Graphics.Display;
using Windows.System.Profile;
using Windows.UI;
using Helpers.Essentials;
using Helpers.Networking;
using static System.Int32;

namespace Helpers {
    public static class SystemInfo {
        private static readonly Dictionary<string, string> WindowsVersionDict = new Dictionary<string, string> {
            {"6.2.9200", "Windows 8"},
            {"6.3.9600", "Windows 8.1"},
            
            {"10.0.18363", "Windows 10 1909"},
            {"10.0.18362", "Windows 10 1903"},
            {"10.0.17763", "Windows 10 1809"},
            {"10.0.17134", "Windows 10 1803"},
            {"10.0.16299", "Windows 10 1709"},
            {"10.0.15063", "Windows 10 1703"},
            {"10.0.14393", "Windows 10 1607"},
            {"10.0.10586", "Windows 10 1511"},
            {"10.0.10240", "Windows 10 1507"},
            
            {"6.3.9600", "Windows Server 2012 R2"},
            {"10.0.14393", "Windows Server 2016, Version 1607"},
            {"10.0.17763", "Windows Server 2019, Version 1809"},
            {"10.0.18363", "Windows Server, Version 1909"},
            {"10.0.19041", "Windows Server, Version 2004"},
            {"10.0.19042", "Windows Server, Version 20H2"},
            {"10.0.20348", "Windows Server 2022"},
            
            {"10.0.10240", "Windows 10, Version 1507"},
            {"10.0.10586", "Windows 10, Version 1511"},
            {"10.0.14393", "Windows 10, Version 1607"},
            {"10.0.15063", "Windows 10, Version 1703"},
            {"10.0.16299", "Windows 10, Version 1709"},
            {"10.0.17134", "Windows 10, Version 1803"},
            {"10.0.17763", "Windows 10, Version 1809"},
            {"10.0.18362", "Windows 10, Version 1903"},
            {"10.0.18363", "Windows 10, Version 1909"},
            {"10.0.19041", "Windows 10, Version 2004"},
            {"10.0.19042", "Windows 10, Version 20H2"},
            {"10.0.19043", "Windows 10, Version 21H1"},
            {"10.0.19044", "Windows 10, Version 21H2"},
            
            {"10.0.22000", "Windows 11, Version 21H2"},
        };
        
        public static string GetSystemVersion(string versionString) {
            versionString = versionString.Substring(0, 10);
            return WindowsVersionDict.ContainsKey(versionString) ? WindowsVersionDict[versionString] : "Unknown Version";
        }

        public static string GetSimpleSystemVersion(string versionString) {
            return $"Windows {versionString.Split(".").FirstOrDefault()}";
        } 

        public static void LogInfo() {
            Logger.Log("[DeviceInfo]", "Idiom:               ", DeviceInfo.Idiom, logLevel: LogLevel.Debug);
            Logger.Log("[DeviceInfo]", "Platform:            ", DeviceInfo.Platform, logLevel: LogLevel.Debug);
            Logger.Log("[DeviceInfo]", "DeviceType:          ", DeviceInfo.DeviceType, logLevel: LogLevel.Debug);
            Logger.Log("[DeviceInfo]", "NetworkAccess:       ", Connectivity.NetworkAccess, logLevel: LogLevel.Debug);
            Logger.Log("[DeviceInfo]", "Display:             ", $"{DisplayInformation.GetForCurrentView().ScreenWidthInRawPixels}x{DisplayInformation.GetForCurrentView().ScreenHeightInRawPixels}, Density: {DisplayInformation.GetForCurrentView().LogicalDpi / 96.0}", logLevel: LogLevel.Debug);
            Logger.Log("[DeviceInfo]", "Name:                ", DeviceInfo.Name, logLevel: LogLevel.Debug);
            Logger.Log("[DeviceInfo]", "Manufacturer:        ", DeviceInfo.Manufacturer, logLevel: LogLevel.Debug);
            Logger.Log("[DeviceInfo]", "Version:             ", $"{GetSystemVersion(DeviceInfo.VersionString)} (Build {DeviceInfo.VersionString})", logLevel: LogLevel.Debug);
            
            // application version
            Logger.Log("[AppInfo]",    "CurrentVersion:      ", AppInfo.VersionString, logLevel: LogLevel.Debug);
            Logger.Log("[AppInfo]",    "FirstLaunchForBuild: ", VersionTracking.IsFirstLaunchForCurrentBuild, logLevel: LogLevel.Debug);
            Logger.Log("[AppInfo]",    "FirstLaunch:         ", VersionTracking.IsFirstLaunchEver, logLevel: LogLevel.Debug);
            
#if DEBUG
            Logger.Log("[AppInfo]",    "AppMode:             ", "Debug", logLevel: LogLevel.Debug);
#else
            Logger.Log("[AppInfo]",    "AppMode:             ", "Release", logLevel: LogLevel.Debug);
#endif
        }

        public static string GetSystemId() {
            var systemId = SystemIdentification.GetSystemIdForPublisher();
            if (systemId.Source == SystemIdentificationSource.None) return "Unknown";
            var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(systemId.Id);
            return dataReader.ReadGuid().ToString();
        }
    }
    
    public static class Time {
        private static readonly DateTime FirstDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long CurrentTimeMillis() {
            return (long) (DateTime.UtcNow - FirstDate).TotalMilliseconds;
        }

        public static DateTime UnixToDatetime(long timestamp) {
            return FirstDate + TimeSpan.FromMilliseconds(timestamp);
        }
    }

    public static class Random {
        private static readonly System.Random SRandom = new System.Random();
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        
        public static object Choice(ArrayList list) => list[SRandom.Next(list.Count)];
        public static T Choice<T>(List<T> list) => list[SRandom.Next(list.Count)];

        public static List<T> Choices<T>(List<T> list, int k) {
            if (k > list.Count) throw new IndexOutOfRangeException();
            if (k == list.Count) return list;
            var result = new List<T>();
            
            while (result.Count != k) {
                var choice = Choice(list);
                if (result.Contains(choice)) continue;
                result.Add(choice);
            }

            return result;
        }

        public static int Int(int min, int max) => SRandom.Next(min, max);
        
        public static string String(int length) => new string(Enumerable.Repeat(Alphabet, length).Select(s => s[SRandom.Next(s.Length)]).ToArray());

        public static bool Bool() => SRandom.Next() > MaxValue / 2;

        public static Color Color() => Windows.UI.Color.FromArgb((byte) Int(0, 256), (byte) Int(0, 256), (byte) Int(0, 256), (byte) Int(0, 256));
        public static Color Color(byte a) => Windows.UI.Color.FromArgb(a, (byte) Int(0, 256), (byte) Int(0, 256), (byte) Int(0, 256));
    }
}