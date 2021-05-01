using System;
using Windows.UI.Xaml.Data;

namespace Helpers.Converters {
    public class BoolToOpacityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) => (bool) value ? 1 : 0.5;

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}