using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AINotes.Controls.FileManagement;
using AINotes.Helpers;
using Helpers;
using Helpers.Extensions;
using MaterialComponents;

namespace AINotes.Controls.Sidebar.Content {
    public class CustomSearchView : Frame, ISidebarView {
        public IEnumerable<MDToolbarItem> ExtraButtons { get; } = new MDToolbarItem[0];

        private readonly CustomFileGridView _documentList;
        public CustomSearchView() {
            var searchBar = new MDEntry {
                Placeholder = ResourceHelper.GetString("search"),
            };
            
            _documentList = new CustomFileGridView {
                Mode = FileGridMode.List
            };
            
            Content = new Grid {
                RowDefinitions = {
                    new RowDefinition {Height = new GridLength(1, GridUnitType.Auto)},
                    new RowDefinition {Height = new GridLength(1, GridUnitType.Star)},
                },
                Children = {
                    {searchBar, 0, 0},
                    {_documentList, 1, 0},
                }
            };

            searchBar.TextChanged += async (_, _) => {
                await SearchFiles(searchBar.Text);
            };
        }

        private readonly SemaphoreSlim _listSemaphore = new SemaphoreSlim(1, 1);
        private async Task SearchFiles(string searchTerm) {
            await _listSemaphore.WaitAsync();
            try {
                _documentList.ModelCollection.Clear();
                var files = await FileHelper.ListFilesReducedAsync();
                _documentList.ModelCollection.AddRange(files.Where(itm => itm.Name.ToLowerInvariant().Contains(searchTerm.ToLowerInvariant())));
            } finally {
                _listSemaphore.Release();
            }
        }
    }
}