using System;
using Windows.UI;

namespace Helpers {
    public static class ColorHelper {
        public static Color Rainbow(float progress) {
            var div = Math.Abs(progress % 1) * 6;
            var ascending = (byte) (int) (div % 1 * 255);
            var descending = (byte) (255 - ascending);

            switch ((int) div) {
                case 0:
                    return Color.FromArgb(255, 255, ascending, 0);
                case 1:
                    return Color.FromArgb(255, descending, 255, 0);
                case 2:
                    return Color.FromArgb(255, 0, 255, ascending);
                case 3:
                    return Color.FromArgb(255, 0, descending, 255);
                case 4:
                    return Color.FromArgb(255, ascending, 0, 255);
                case 5:
                    return Color.FromArgb(255, 255, 0, descending);
                default:
                    return Colors.White;
            }
        }
    }
}