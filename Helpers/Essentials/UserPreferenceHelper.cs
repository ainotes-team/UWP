namespace Helpers.Essentials {
    public static class UserPreferenceHelper {
        private const string Identifier = "user_";

        public static void Set(string key, string value) => AppPreferencesHelper.Set(Identifier + key, value);
        public static void Set(string key, bool value) => AppPreferencesHelper.Set(Identifier + key, value);
        public static void Set(string key, int value) => AppPreferencesHelper.Set(Identifier + key, value);
        public static void Set(string key, double value) => AppPreferencesHelper.Set(Identifier + key, value);
        public static void Set(string key, float value) => AppPreferencesHelper.Set(Identifier + key, value);
        public static void Set(string key, long value) => AppPreferencesHelper.Set(Identifier + key, value);

        public static string Get(string key, string defaultValue) => AppPreferencesHelper.Get(Identifier + key, defaultValue);
        public static bool Get(string key, bool defaultValue) => AppPreferencesHelper.Get(Identifier + key, defaultValue);
        public static int Get(string key, int defaultValue) => AppPreferencesHelper.Get(Identifier + key, defaultValue);
        public static double Get(string key, double defaultValue) => AppPreferencesHelper.Get(Identifier + key, defaultValue);
        public static float Get(string key, float defaultValue) => AppPreferencesHelper.Get(Identifier + key, defaultValue);
        public static long Get(string key, long defaultValue) => AppPreferencesHelper.Get(Identifier + key, defaultValue);
    }
}