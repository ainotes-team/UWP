using System;
using Windows.UI.Xaml.Data;

namespace AINotes.Helpers.Converters {
    public class UserIdToNameConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            var userId = (string) value;
            if (userId == CloudAdapter.CurrentRemoteUserModel?.RemoteId) {
                return "me";
            }
            return CloudAdapter.GetCachedRemoteUser(userId)?.DisplayName ?? "Loading...";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}