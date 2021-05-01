using System;
using Windows.UI.Xaml.Media;
using Helpers.Extensions;

namespace MaterialComponents {
    public struct Theme {
        // General
        public Brush Background;
        public Brush Text;
        
        // Buttons
        public Brush ButtonBackgroundPrimary;
        public Brush ButtonForegroundPrimary;
        public Brush ButtonBorderPrimary;
        
        public Brush ButtonBackgroundSecondary;
        public Brush ButtonForegroundSecondary;
        public Brush ButtonBorderSecondary;
        
        public Brush ButtonBackgroundDisabled;
        public Brush ButtonForegroundDisabled;
        public Brush ButtonBorderDisabled;
        
        // Cards
        public Brush CardBorder;
            
        // Toolbar Items
        public Brush TBIDefault;
        public Brush TBIHover;
        public Brush TBITap;
    }
    
    public static class Theming {
        public static event Action ThemeChanged;
        
        public static Theme CurrentTheme { get; private set; } = new Theme {
            // General
            Background = ColorCreator.FromHex("#FFFFFF").ToBrush(),
            Text = ColorCreator.FromHex("#444444").ToBrush(),

            // Buttons
            ButtonBackgroundPrimary = ColorCreator.FromHex("#1A73E8").ToBrush(),
            ButtonForegroundPrimary = ColorCreator.FromHex("#FFFFFF").ToBrush(),
            ButtonBorderPrimary = ColorCreator.FromHex("#1A73E8").ToBrush(),
            
            ButtonBackgroundSecondary = ColorCreator.FromHex("#FFFFFF").ToBrush(),
            ButtonForegroundSecondary = ColorCreator.FromHex("#1A73E8").ToBrush(),
            ButtonBorderSecondary = ColorCreator.FromHex("#DADCE0").ToBrush(),
            
            ButtonBackgroundDisabled = ColorCreator.FromHex("#FFFFFF").ToBrush(),
            ButtonForegroundDisabled = ColorCreator.FromHex("#DADCE0").ToBrush(),
            ButtonBorderDisabled = ColorCreator.FromHex("#DADCE0").ToBrush(),
            
            // Cards
            CardBorder = ColorCreator.FromHex("#DADCE0").ToBrush(),
            
            // Toolbar Items
            TBIDefault = ColorCreator.FromHex("#00FFFFFF").ToBrush(),
            TBIHover = ColorCreator.FromHex("#DADCE0").ToBrush(),
            TBITap = ColorCreator.FromHex("#696969").ToBrush(),
            
        };

        public static void SetTheme(Theme theme) {
            CurrentTheme = theme;
            ThemeChanged?.Invoke();
        }
    }
}