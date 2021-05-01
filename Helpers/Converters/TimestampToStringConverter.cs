using System;
using System.Globalization;
using Windows.UI.Xaml.Data;

namespace Helpers.Converters {
    public class TimestampToStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            var timestamp = (long) value;
            var datetime = Time.UnixToDatetime(timestamp);
            return datetime.ToLocalTime().ToString(CultureInfo.CurrentCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}