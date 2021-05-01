using System;
using Windows.Storage;

namespace Helpers.Essentials {
    public static class AppPreferencesHelper {
        private static readonly object Locker = new object();

        public static bool ContainsKey(string key, string sharedName) => InternalContainsKey(key, sharedName);
        
        public static string Get(string key, string defaultValue, string sharedName) => InternalGet(key, defaultValue, sharedName);
        public static void Set(string key, string value, string sharedName) => InternalSet(key, value, sharedName);
        
        public static string Get(string key, string defaultValue) => InternalGet(key, defaultValue, null);
        public static bool Get(string key, bool defaultValue) => InternalGet(key, defaultValue, null);
        public static int Get(string key, int defaultValue) => InternalGet(key, defaultValue, null);
        public static double Get(string key, double defaultValue) => InternalGet(key, defaultValue, null);
        public static float Get(string key, float defaultValue) => InternalGet(key, defaultValue, null);
        public static long Get(string key, long defaultValue) => InternalGet(key, defaultValue, null);

        public static void Set(string key, string value) => InternalSet(key, value, null);
        public static void Set(string key, bool value) => InternalSet(key, value, null);
        public static void Set(string key, int value) => InternalSet(key, value, null);
        public static void Set(string key, double value) => InternalSet(key, value, null);
        public static void Set(string key, float value) => InternalSet(key, value, null);
        public static void Set(string key, long value) => InternalSet(key, value, null);


        private static bool InternalContainsKey(string key, string sharedName) {
            lock (Locker) {
                var appDataContainer = GetApplicationDataContainer(sharedName);
                return appDataContainer.Values.ContainsKey(key);
            }
        }
        
        private static void InternalSet<T>(string key, T value, string sharedName) {
            Logger.Log("[AppPreferencesHelper]", $"InternalSet: {key} => {value}{(sharedName != null ? $" ({sharedName})" : "")}");
            
            lock (Locker) {
                var appDataContainer = GetApplicationDataContainer(sharedName);

                if (value == null) {
                    if (appDataContainer.Values.ContainsKey(key)) appDataContainer.Values.Remove(key);
                    return;
                }

                try {
                    appDataContainer.Values[key] = value;
                } catch (Exception ex) {
                    Logger.Log("[AppPreferencesHelper]", "InternalSet - Exception:", ex.ToString(), logLevel: LogLevel.Error);
                }
            }
        }

        private static T InternalGet<T>(string key, T defaultValue, string sharedName) {
            lock (Locker) {
                
                try {
                    var appDataContainer = GetApplicationDataContainer(sharedName);
                    if (!appDataContainer.Values.ContainsKey(key)) return defaultValue;
                    var tempValue = appDataContainer.Values[key];
                    if (tempValue != null) return (T) tempValue;
                } catch (Exception ex) {
                    Logger.Log("[AppPreferencesHelper]", "InternalGet - Exception:", ex.ToString(), logLevel: LogLevel.Error);
                }
            }

            return defaultValue;
        }

        private static ApplicationDataContainer GetApplicationDataContainer(string sharedName) {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (string.IsNullOrWhiteSpace(sharedName)) return localSettings;

            if (!localSettings.Containers.ContainsKey(sharedName)) localSettings.CreateContainer(sharedName, ApplicationDataCreateDisposition.Always);

            return localSettings.Containers[sharedName];
        }
    }
}