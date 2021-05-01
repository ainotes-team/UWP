using System;
using System.Globalization;
using Windows.UI.Xaml.Data;

namespace Helpers.Converters {
    public class UnixTimeToStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            var dtDateTime = new DateTime(1970,1,1,0,0,0,0, DateTimeKind.Utc).AddSeconds((long) value).ToLocalTime();
            return dtDateTime.ToString("d", CultureInfo.InstalledUICulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}