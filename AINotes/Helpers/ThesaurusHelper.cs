using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Helpers;
using SQLite;

namespace AINotes.Helpers {
    public class ThesaurusModel {
        public string Word { get; set; }
        public string Synonym { get; set; }
    }

    public static class ThesaurusHelper {
        private static readonly SQLiteAsyncConnection ThesaurusDatabase = new SQLiteAsyncConnection(LocalFileHelper.ToAbsolutePath("openthesaurus.db3"));

        static ThesaurusHelper() {
            Task.Run(async () => {
                var databaseExists = LocalFileHelper.FileExists("openthesaurus.db3");
                if (!databaseExists) {
                    var databaseFile = await (await Package.Current.InstalledLocation.GetFolderAsync("Assets")).GetFileAsync("openthesaurus.db3");
                    await databaseFile.CopyAsync(ApplicationData.Current.LocalFolder);
                }
            }).Wait();
        }

        public static async Task<List<ThesaurusModel>> FindSynonyms(string term, int lmt=25) {
            return await ThesaurusDatabase.Table<ThesaurusModel>().Where(itm => itm.Word.StartsWith(term)).Take(lmt).ToListAsync();
        }
    }
}