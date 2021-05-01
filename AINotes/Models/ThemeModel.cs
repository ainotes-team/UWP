using Windows.UI.Xaml.Media;
using AINotes.Helpers;
using Newtonsoft.Json;

namespace AINotes.Models {
    public class ThemeModel {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("background"), JsonConverter(typeof(ColorBrushConverter))]
        public SolidColorBrush Background { get; set; }

        [JsonProperty("foreground"), JsonConverter(typeof(ColorBrushConverter))]
        public SolidColorBrush Foreground { get; set; }

        [JsonProperty("text"), JsonConverter(typeof(ColorBrushConverter))]
        public SolidColorBrush Text { get; set; }

        [JsonProperty("toolbar"), JsonConverter(typeof(ColorBrushConverter))]
        public SolidColorBrush Toolbar { get; set; }

        [JsonProperty("divider"), JsonConverter(typeof(ColorBrushConverter))]
        public SolidColorBrush Divider { get; set; }

        [JsonProperty("sidebar_background"), JsonConverter(typeof(ColorBrushConverter))]
        public SolidColorBrush SidebarBackground { get; set; }

        [JsonProperty("card_background"), JsonConverter(typeof(ColorBrushConverter))]
        public SolidColorBrush CardBackground { get; set; }

        [JsonProperty("card_border"), JsonConverter(typeof(ColorBrushConverter))]
        public SolidColorBrush CardBorder { get; set; }

        [JsonProperty("component_border"), JsonConverter(typeof(ColorBrushConverter))]
        public SolidColorBrush ComponentBorder { get; set; }

        [JsonProperty("component_background"), JsonConverter(typeof(ColorBrushConverter))]
        public SolidColorBrush ComponentBackground { get; set; }

        [JsonProperty("component_nob_border"), JsonConverter(typeof(ColorBrushConverter))]
        public SolidColorBrush ComponentNobBorder { get; set; }

        [JsonProperty("component_nob_background"), JsonConverter(typeof(ColorBrushConverter))]
        public SolidColorBrush ComponentNobBackground { get; set; }

        [JsonProperty("toolbar_item_hover"), JsonConverter(typeof(ColorBrushConverter))]
        public SolidColorBrush ToolbarItemHover { get; set; }

        [JsonProperty("toolbar_item_tap"), JsonConverter(typeof(ColorBrushConverter))]
        public SolidColorBrush ToolbarItemTap { get; set; }
    }
}