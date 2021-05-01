using System;
using Windows.UI.Xaml.Data;

namespace Helpers.Converters {
    public class StringSuffixConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            var str = value is string s ? s : value.ToString();
            var suffix = (string) parameter;

            return str + suffix;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}