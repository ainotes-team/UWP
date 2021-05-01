using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.UI.Xaml.Data;

namespace Helpers.Converters {
    public class ValueConverterGroup : List<IValueConverter>, IValueConverter {
        private string[] _parameters;

        public object Convert(object value, Type targetType, object parameter, string language) {
            if (parameter != null) _parameters = Regex.Split(parameter.ToString(), @"(?<!\\),");

            return this.Aggregate(value, (current, converter) => converter.Convert(current, targetType, GetParameter(converter), language));
        }
        
        private string GetParameter(IValueConverter converter) {
            if (_parameters == null) return null;

            var index = IndexOf(converter);
            string parameter;

            try {
                parameter = _parameters[index];
            } catch (IndexOutOfRangeException) {
                parameter = null;
            }

            if (parameter != null) parameter = Regex.Unescape(parameter);

            return parameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}