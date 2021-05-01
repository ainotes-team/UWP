using System;
using Windows.UI.Xaml.Data;

namespace Helpers.Converters {
    public class StringToInitialConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            var chars = (value as string)?.ToCharArray();
            return chars == null || chars.Length == 0 ? "" : chars[0].ToString().ToUpperInvariant();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}