using System;
using Windows.UI.Xaml.Data;

namespace Helpers.Converters {
    public class LoggerConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            Logger.Log("[LoggerConverter]", $"Value: {value} | Type {targetType} | Parameter {parameter} | Language {language}");
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}