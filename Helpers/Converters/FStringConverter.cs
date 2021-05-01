using System;
using System.Collections;
using Windows.UI.Xaml.Data;
using Helpers.Extensions;

namespace Helpers.Converters {
    public class FStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) => ((IEnumerable) value).ToFString();

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}