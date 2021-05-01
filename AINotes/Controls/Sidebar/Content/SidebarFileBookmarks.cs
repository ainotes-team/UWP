using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using AINotes.Controls.Pages;
using AINotes.Controls.Popups;
using AINotes.Helpers;
using Helpers;
using Helpers.Extensions;
using AINotes.Models;
using MaterialComponents;

namespace AINotes.Controls.Sidebar.Content {
    public class SidebarFileBookmarks : Frame, ISidebarView {
        private IEnumerable<MDToolbarItem> _extraButtons;
        public IEnumerable<MDToolbarItem> ExtraButtons {
            get {
                if (_extraButtons != null) return _extraButtons;

                var addItem = new MDToolbarItem {
                    ImageSource = new BitmapImage(new Uri(Icon.Add)),
                };

                addItem.Released += OnAddTBIReleased;

                _extraButtons = new List<MDToolbarItem> {
                    addItem
                };

                return _extraButtons;
            }
        }

        private readonly ObservableCollection<FileBookmarkModel> _bmSource = new ObservableCollection<FileBookmarkModel>();

        private int _currentFileId;

        private async void CreateBookmark() {
            _bmSource.Add(new FileBookmarkModel {
                Name = "Unnamed",
                ScrollX = App.EditorScreen?.ScrollX ?? 0,
                ScrollY = App.EditorScreen?.ScrollY ?? 0,
                Zoom = App.EditorScreen?.ScrollZoom ?? 1,
            });
            await FileHelper.SetFileBookmarks(_currentFileId, _bmSource);
        }

        private void OnAddTBIReleased(object s, EventArgs e) {
            CreateBookmark();
        }
        
        private bool _isFirstOverride = true;
        protected override Size ArrangeOverride(Size finalSize) {
            if (!_isFirstOverride) return base.ArrangeOverride(finalSize);
            Content = new ListView {
                // ItemTemplate = SidebarContentResources.TaskTemplate,
                SelectionMode = ListViewSelectionMode.None,
                Transitions = new TransitionCollection(),
                ItemContainerTransitions = new TransitionCollection(),
                ItemContainerStyle = SidebarContentResources.SimpleListViewStyle,
                ItemsSource = _bmSource,
                IsItemClickEnabled = true,
            };

            void RightTappedHandler(object sender, RightTappedRoutedEventArgs args) {
                var itm = (FileBookmarkModel) ((FrameworkElement)args.OriginalSource).DataContext;
                if (itm == null) return;
                    
                var p = args.GetPosition(Window.Current.Content);
                var entry = new MDEntry();
                    
                CustomDropdown.ShowDropdown(new List<CustomDropdownViewTemplate> {
                    new CustomDropdownItem("Rename", () => {
                        CustomDropdown.CloseDropdown();
                        new MDContentPopup("Rename", entry, async () => {
                            Logger.Log("Rename", itm.Name, "=>", entry.Text);

                            var sIdx = _bmSource.IndexOf(itm);
                                
                            var item = _bmSource.FirstOrDefault(i => _bmSource.IndexOf(i) == sIdx);
                            if (item != null) {
                                item.Name = entry.Text;
                            }
                                
                            await FileHelper.SetFileBookmarks(_currentFileId, _bmSource);

                            if (!(Content is ListView lv)) return;
                            lv.ItemsSource = null;
                            lv.ItemsSource = _bmSource;
                        }, cancelable: true, closeWhenBackgroundIsClicked: true, closeOnOk: true).Show();
                    }),
                    new CustomDropdownItem("Delete", async () => {
                        _bmSource.Remove(itm);
                            
                        await FileHelper.SetFileBookmarks(_currentFileId, _bmSource);

                        if (!(Content is ListView lv)) return;
                        lv.ItemsSource = null;
                        lv.ItemsSource = _bmSource;
                    })
                }, p);
            }


            static void ItemClickHandler(object sender, ItemClickEventArgs args) {
                var itm = (FileBookmarkModel) args.ClickedItem;
                App.EditorScreen.ChangeScrollView(itm.ScrollX, itm.ScrollY, itm.Zoom);
            }

            ((ListView) Content).RightTapped += RightTappedHandler;
            ((ListView) Content).ItemClick += ItemClickHandler;

            if (App.Page.Content == App.EditorScreen) {
                try {
                    var values = App.EditorScreen.LoadedFileModel.SerializedBookmarks?.Deserialize<List<FileBookmarkModel>>();
                    if (values != null) {
                        _bmSource.AddRange(values);
                    }
                    _currentFileId = App.EditorScreen.LoadedFileModel.FileId;
                } catch (Exception ex) {
                    Logger.Log("[SidebarFileBookmarks]", "LoadingFile Initial Handler Exception:", ex.ToString());
                }
            }

            App.EditorScreen.LoadingFile += fileModel => {
                _bmSource.Clear();
                try {
                    var values = fileModel.SerializedBookmarks?.Deserialize<List<FileBookmarkModel>>();
                    if (values != null) {
                        _bmSource.AddRange(values);
                    }
                    _currentFileId = fileModel.FileId;
                    Logger.Log("LoadingFile", _currentFileId);
                } catch (Exception ex) {
                    Logger.Log("[SidebarFileBookmarks]", "LoadingFile Handler Exception:", ex.ToString());
                }
            };
            _isFirstOverride = false;

            return base.ArrangeOverride(finalSize);
        }
    }
}