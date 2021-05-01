namespace Helpers {
    public static class ResourceHelper {
        public static string GetString(string resource) {
            var result = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString(resource);
            // AINotes.Helpers.Essentials.Logger.Log("[StringsHelper]", $"GetString {resource} -> {result}");
            return string.IsNullOrEmpty(result) ? $"Resource \"{resource}\" not found." : result;
        }
        
        public static string GetString(string context, string resource) {
            var result = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView(context).GetString(resource);
            // AINotes.Helpers.Essentials.Logger.Log("[StringsHelper]", $"GetString {context}: {resource} -> {result}");
            return string.IsNullOrEmpty(result) ? $"Resource \"{context}->{resource}\" not found." : result;
        }
    }
}