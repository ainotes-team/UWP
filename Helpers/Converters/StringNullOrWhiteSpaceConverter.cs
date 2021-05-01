using System;
using Windows.UI.Xaml.Data;

namespace Helpers.Converters {
    public class StringNullOrWhiteSpaceConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            var str = (string) value;
            var nullOrEmptyNotice = (string) parameter;

            return string.IsNullOrWhiteSpace(str) ? nullOrEmptyNotice : str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}