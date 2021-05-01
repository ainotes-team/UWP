using System;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace Helpers.Extensions {
    public struct HslColor {
        public double H;
        public double S;
        public double L;
        public double A;
    }

    public static class ColorExtensions {
        public static Color AddLuminosity(this Color color, double delta) {
            var hslColor = color.ToHsl();
            return ColorCreator.FromHsl(hslColor.H, hslColor.S, hslColor.L + delta, hslColor.A);
        }

        public static Brush AddLuminosity(this Brush solidColorBrush, double delta) {
            var hslColor = ((SolidColorBrush) solidColorBrush).Color.ToHsl();
            return ColorCreator.FromHsl(hslColor.H, hslColor.S, hslColor.L + delta, hslColor.A).ToBrush();
        }

        // ReSharper disable once UseDeconstructionOnParameter
        public static void Deconstruct(this System.Drawing.Color color, out double r, out double g, out double b) {
            r = color.R;
            g = color.G;
            b = color.B;
        }

        public static SolidColorBrush ToBrush(this Color color) {
            return new SolidColorBrush(color);
        }

        public static SolidColorBrush ToBrush(this Color color, double opacity) {
            return new SolidColorBrush {
                Color = color,
                Opacity = opacity
            };
        }

        private static float Merge(this float start, float end, float amount) {
            var difference = end - start;
            var adjusted = difference * amount;
            return start + adjusted;
        }

        public static string ToHex(this Color color) {
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        public static HslColor ToHsl(this Color color) {
            const double toDouble = 1.0 / 255;
            var r = toDouble * color.R;
            var g = toDouble * color.G;
            var b = toDouble * color.B;
            var max = Math.Max(Math.Max(r, g), b);
            var min = Math.Min(Math.Min(r, g), b);
            var chroma = max - min;
            double h1;

            if (chroma == 0) {
                h1 = 0;
            } else if (max == r) {
                h1 = ((g - b) / chroma + 6) % 6;
            } else if (max == g) {
                h1 = 2 + (b - r) / chroma;
            } else {
                h1 = 4 + (r - g) / chroma;
            }

            var lightness = 0.5 * (max + min);
            var saturation = chroma == 0 ? 0 : chroma / (1 - Math.Abs(2 * lightness - 1));
            HslColor ret;
            ret.H = 60 * h1;
            ret.S = saturation;
            ret.L = lightness;
            ret.A = toDouble * color.A;
            return ret;
        }

        public static Color Merge(this Color colour, Color to, float amount) {
            float sr = colour.R, sg = colour.G, sb = colour.B;
            float er = to.R, eg = to.G, eb = to.B;
            byte r = (byte) sr.Merge(er, amount), g = (byte) sg.Merge(eg, amount), b = (byte) sb.Merge(eb, amount);
            return Color.FromArgb(255, r, g, b);
        }
    }

    public static class ColorCreator {
        public static Color FromHex(string hex) {
            hex = hex.Replace("#", string.Empty);
            switch (hex.Length) {
                // RGB
                case 6: {
                    var r = (byte) Convert.ToUInt32(hex.Substring(0, 2), 16);
                    var g = (byte) Convert.ToUInt32(hex.Substring(2, 2), 16);
                    var b = (byte) Convert.ToUInt32(hex.Substring(4, 2), 16);
                    return Color.FromArgb(255, r, g, b);
                }
                // ARGB
                case 8: {
                    var a = (byte) Convert.ToUInt32(hex.Substring(0, 2), 16);
                    var r = (byte) Convert.ToUInt32(hex.Substring(2, 2), 16);
                    var g = (byte) Convert.ToUInt32(hex.Substring(4, 2), 16);
                    var b = (byte) Convert.ToUInt32(hex.Substring(6, 2), 16);
                    return Color.FromArgb(a, r, g, b);
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(hex), hex, @"hex out of range");
            }
        }

        public static Color FromHsl(double hue, double saturation, double lightness, double alpha = 1.0) {
            if (hue < 0 || hue > 360) {
                throw new ArgumentOutOfRangeException(nameof(hue));
            }

            var chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
            var h1 = hue / 60;
            var x = chroma * (1 - Math.Abs(h1 % 2 - 1));
            var m = lightness - 0.5 * chroma;
            double r1, g1, b1;

            if (h1 < 1) {
                r1 = chroma;
                g1 = x;
                b1 = 0;
            } else if (h1 < 2) {
                r1 = x;
                g1 = chroma;
                b1 = 0;
            } else if (h1 < 3) {
                r1 = 0;
                g1 = chroma;
                b1 = x;
            } else if (h1 < 4) {
                r1 = 0;
                g1 = x;
                b1 = chroma;
            } else if (h1 < 5) {
                r1 = x;
                g1 = 0;
                b1 = chroma;
            } else {
                r1 = chroma;
                g1 = 0;
                b1 = x;
            }

            var r = (byte) (255 * (r1 + m));
            var g = (byte) (255 * (g1 + m));
            var b = (byte) (255 * (b1 + m));
            var a = (byte) (255 * alpha);

            return Color.FromArgb(a, r, g, b);
        }
    }
}