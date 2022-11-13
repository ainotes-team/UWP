using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using AINotes.Controls.FileManagement;
using AINotes.Helpers;
using Helpers.Extensions;
using MaterialComponents;

namespace AINotes.Controls.Sidebar.Content {
    public class CustomRecentFilesView : Frame, ISidebarView {
        private IEnumerable<MDToolbarItem> _extraButtons;
        public IEnumerable<MDToolbarItem> ExtraButtons {
            get {
                if (_extraButtons != null) return _extraButtons;
                
                var reloadTBI = new MDToolbarItem {
                    ImageSource = new BitmapImage(new Uri(Icon.Reset)),
                };
                reloadTBI.Released += OnReloadTBIReleased;
                    
                _extraButtons = new List<MDToolbarItem> {
                    reloadTBI
                };

                return _extraButtons;
            }
        }

        private readonly CustomFileGridView _documentList;
        public CustomRecentFilesView() {
            _documentList = new CustomFileGridView {
                Mode = FileGridMode.List
            };
            
            Content = _documentList;

            LoadRecent();
        }

        private void OnReloadTBIReleased(object s, EventArgs e) {
            LoadRecent();
        }

        private async void LoadRecent() {
            _documentList.ModelCollection.Clear();
            var files = await FileHelper.ListFilesReducedAsync();
            files.Sort((x, y) => x.LastChangedDate.CompareTo(y.LastChangedDate));
            files.Reverse();
            files = files.GetRange(0, Math.Min(Preferences.MaxRecentFilesShown, files.Count));
            _documentList.ModelCollection.AddRange(files);
        }
    }
}