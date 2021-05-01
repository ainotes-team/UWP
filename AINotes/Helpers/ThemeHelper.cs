using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Windows.UI.Xaml.Media;
using AINotes.Models;
using Helpers;
using Helpers.Extensions;
using Newtonsoft.Json;

namespace AINotes.Helpers {
    public class ColorBrushConverter : JsonConverter<SolidColorBrush> {
        public override void WriteJson(JsonWriter writer, SolidColorBrush value, JsonSerializer serializer) => throw new ConstraintException();
        public override SolidColorBrush ReadJson(JsonReader reader, Type objectType, SolidColorBrush existingValue, bool hasExistingValue, JsonSerializer serializer) => ColorCreator.FromHex(reader?.Value?.ToString()).ToBrush();
    }

    public static class ThemeHelper {
        private static IEnumerable<ThemeModel> _themes;
        public static IEnumerable<ThemeModel> Themes => _themes ??= LoadThemes();

        private static IEnumerable<ThemeModel> LoadThemes() {
            if (!LocalFileHelper.FolderExists("themes")) {
                LocalFileHelper.CreateFolder("themes");
            }

            if (!LocalFileHelper.GetFiles("themes").Contains("default.json")) {
                LocalFileHelper.WriteFile("themes/default.json", "{\"name\":\"Default\",\"author\":\"Vincent Schmandt\",\"background\":\"#FFFFFF\",\"foreground\":\"#444444\",\"text\":\"#444444\",\"toolbar\":\"#FFFFFF\",\"divider\":\"#DADCE0\",\"sidebar_background\":\"#F4F4F4\",\"card_background\":\"#DADCE0\",\"card_border\":\"#DADCE0\",\"component_border\":\"#444444\",\"component_background\":\"#00FFFFFF\",\"component_nob_border\":\"#444444\",\"component_nob_background\":\"#FFFFFF\",\"toolbar_item_hover\":\"#DADCE0\",\"toolbar_item_tap\":\"#A7A9AC\"}");
            }
            
            if (!LocalFileHelper.GetFiles("themes").Contains("dark.json")) {
                LocalFileHelper.WriteFile("themes/dark.json", "{\"name\": \"Dark\",\"author\": \"Vincent Schmandt\",\"background\": \"#252423\",\"foreground\": \"#B5B3B2\",\"text\": \"#B5B3B2\",\"toolbar\": \"#323130\",\"divider\": \"#1B1A19\",\"sidebar_background\": \"#252423\",\"card_background\": \"#1B1A19\",\"card_border\": \"#444444\",\"component_border\": \"#444444\",\"component_background\": \"#00FFFFFF\",\"component_nob_border\": \"#444444\",\"component_nob_background\": \"#C2C2C2\",\"toolbar_item_hover\": \"#DADCE0\",\"toolbar_item_tap\": \"#A7A9AC\"}");
            }

            return LocalFileHelper.GetFiles("themes").ToList().ConvertAll(filePath => JsonConvert.DeserializeObject<ThemeModel>(LocalFileHelper.ReadFile("themes/" + filePath)));
        }

        public static ThemeModel GetTheme() => Themes.First(t => t.Name == Preferences.DisplayTheme);
    }
}