using System.Collections.Generic;
using System.Globalization;

namespace Helpers.Essentials {
    public static class CultureHelper {
        public static CultureInfo FromDisplayName(string cultureInfoName) {
            var languageCultureDict = new Dictionary<string, string> {
                {"Deutsch", "de-DE"},
                {"German", "de-DE"},
                {"Englisch", "en-US"},
                {"English", "en-US"},
                {"English (United States)", "en-US"},
            };
            return languageCultureDict.ContainsKey(cultureInfoName) ? new CultureInfo(languageCultureDict[cultureInfoName]) : new CultureInfo("en-US");
        }
    }
}