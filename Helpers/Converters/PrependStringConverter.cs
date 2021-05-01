using System;
using Windows.UI.Xaml.Data;

namespace Helpers.Converters {
    public class PrependStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            return (string) parameter + " " + (string) value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();

    }
}