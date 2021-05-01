namespace Helpers.Essentials {
    public static class BaseUrl {
        public static string GetAppPackage() => "ms-appx:///";
        public static string GetAppDataLocal() => "ms-appdata:///local";
        public static string GetImages() =>  "ms-appx:///Assets/Images/";
    }
}