using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using AINotes.Controls.Pages;
using AINotes.Controls.Popups;
using AINotes.Helpers;
using AINotes.Helpers.Extensions;
using AINotes.Helpers.Imaging;
using Helpers;
using Helpers.Essentials;
using Helpers.Extensions;
using Helpers.Lists;
using AINotes.Models;
using AINotesCloud;
using AINotesCloud.Models;
using MaterialComponents;
using Colors = Windows.UI.Colors;

namespace AINotes.Controls.FileManagement {
    public enum FileGridMode {
        Grid,
        List,
    }

    public enum SortByMode {
        Alphabetical,
        LastEdited,
        LastCreated,
        Label,
        Owner,
        Status,
    }

    public class FileDirectoryTemplateSelector : DataTemplateSelector {
        public DataTemplate File { get; set; }
        public DataTemplate Directory { get; set; }

        protected override DataTemplate SelectTemplateCore(object item) {
            switch (item) {
                case FileModel _:
                    return File;
                case DirectoryModel _:
                    return Directory;
                default:
                    throw new ArgumentOutOfRangeException(nameof(item), item, null);
            }
        }
    }

    public sealed partial class CustomFileGridView {
        public List<IFMSListableModel> SelectedModels => ItemGrid.SelectedItems.Cast<IFMSListableModel>().ToList();
        public readonly ObservableList<IFMSListableModel> ModelCollection = new ObservableList<IFMSListableModel>();
        
        private FileGridMode _mode;
        public FileGridMode Mode {
            get => _mode;
            set {
                _mode = value;
                if (InternalItemWrap == null) return;
                if (value != _mode) {
                    foreach (var range in ItemGrid.SelectedRanges) {
                        ItemGrid.DeselectRange(range);
                    }

                    CustomDropdown.CloseDropdown();
                }

                switch (value) {
                    case FileGridMode.Grid:
                        InternalItemWrap.MaximumRowsOrColumns = -1;
                        InternalItemWrap.ItemWidth = double.NaN;
                        // ItemGrid.ItemTemplate = GridModeTemplate;
                        break;
                    case FileGridMode.List:
                        InternalItemWrap.MaximumRowsOrColumns = 1;
                        InternalItemWrap.ItemWidth = InternalItemWrap.RenderSize.Width;
                        // ItemGrid.ItemTemplate = ListModeTemplate;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }
        }

        public ItemsWrapGrid InternalItemWrap => (ItemsWrapGrid) FindName("ItemWrap");

        public CustomFileGridView() {
            InitializeComponent();
            
            Mode = _mode;

            ToggleSortByNameTBI.PointerEntered += (_, _) => ToggleSortByNameTBI.Opacity = 1.0;
            ToggleSortByNameTBI.PointerExited += (_, _) => ToggleSortByNameTBI.Opacity = SortByMode == SortByMode.Alphabetical ? 1.0 : 0.0;
            ToggleSortByNameTBI.Pressed += (_, _) => {
                SortBy(SortByMode.Alphabetical, SortByMode != SortByMode.Alphabetical || !SortByModeDescending);
            };

            ToggleSortByOwnerTBI.PointerEntered += (_, _) => ToggleSortByOwnerTBI.Opacity = 1.0;
            ToggleSortByOwnerTBI.PointerExited += (_, _) => ToggleSortByOwnerTBI.Opacity = SortByMode == SortByMode.Owner ? 1.0 : 0.0;
            ToggleSortByOwnerTBI.Pressed += (_, _) => {
                SortBy(SortByMode.Owner, SortByMode != SortByMode.Owner || !SortByModeDescending);
            };
            
            ToggleSortByStatusTBI.PointerEntered += (_, _) => ToggleSortByStatusTBI.Opacity = 1.0;
            ToggleSortByStatusTBI.PointerExited += (_, _) => ToggleSortByStatusTBI.Opacity = SortByMode == SortByMode.Status ? 1.0 : 0.0;
            ToggleSortByStatusTBI.Pressed += (_, _) => {
                SortBy(SortByMode.Status, SortByMode != SortByMode.Status || !SortByModeDescending);
            };
            
            ToggleSortByCreatedTBI.PointerEntered += (_, _) => ToggleSortByCreatedTBI.Opacity = 1.0;
            ToggleSortByCreatedTBI.PointerExited += (_, _) => ToggleSortByCreatedTBI.Opacity = SortByMode == SortByMode.LastCreated ? 1.0 : 0.0;
            ToggleSortByCreatedTBI.Pressed += (_, _) => {
                SortBy(SortByMode.LastCreated, SortByMode != SortByMode.LastCreated || !SortByModeDescending);
            };
            
            ToggleSortByLabelsTBI.PointerEntered += (_, _) => ToggleSortByLabelsTBI.Opacity = 1.0;
            ToggleSortByLabelsTBI.PointerExited += (_, _) => ToggleSortByLabelsTBI.Opacity = SortByMode == SortByMode.Label ? 1.0 : 0.0;
            ToggleSortByLabelsTBI.Pressed += (_, _) => {
                SortBy(SortByMode.Label, SortByMode != SortByMode.Label || !SortByModeDescending);
            };
        }
        
        private SortByMode _sortByMode;
        public SortByMode SortByMode {
            get => _sortByMode;
            set {    
                _sortByMode = value;
                ModelCollection.SortingSelector = i => i.GetType().ToString();
                switch (_sortByMode) {
                    case SortByMode.Alphabetical:
                        ModelCollection.SecondarySortingSelector = i => i.Name;
                        break;
                    case SortByMode.LastEdited:
                        ModelCollection.SecondarySortingSelector = i => i.LastChangedDate;
                        break;
                    case SortByMode.LastCreated:
                        ModelCollection.SecondarySortingSelector = i => i.CreationDate;
                        break;
                    case SortByMode.Label:
                        ModelCollection.SecondarySortingSelector = i => i.Labels.Count != 0 ? i.Labels.Min() : 0;
                        break;
                    case SortByMode.Owner:
                        ModelCollection.SecondarySortingSelector = i => i.Owner;
                        break;
                    case SortByMode.Status:
                        ModelCollection.SecondarySortingSelector = i => i.Status;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }
        }
        
        public bool SortByModeDescending => ModelCollection.Descending;

        public void SortBy(SortByMode mode, bool descending) {
            Logger.Log("[CustomFileGridView]", "SortBy", mode, descending);
            
            SortByMode = mode;
            ModelCollection.Descending = descending;
            
            SavedStatePreferenceHelper.Set("FileContainerFileSortByMode", (int) mode);
            SavedStatePreferenceHelper.Set("FileContainerFileSortByModeDescending", descending);

            const double lowVisibilityAlpha = 0.2;
            const double highVisibilityAlpha = 1.0;
            switch (SortByMode) {
                case SortByMode.Alphabetical:
                    MainThread.BeginInvokeOnMainThread(() => {
                        ToggleSortByStatusTBI.Opacity = lowVisibilityAlpha;
                        ToggleSortByNameTBI.Opacity = highVisibilityAlpha;
                        ToggleSortByOwnerTBI.Opacity = lowVisibilityAlpha;
                        ToggleSortByCreatedTBI.Opacity = lowVisibilityAlpha;
                        ToggleSortByLabelsTBI.Opacity = lowVisibilityAlpha;
                        
                        ToggleSortByNameTBI.ImageSource = !descending ? ImageSourceHelper.FromName(Icon.ExpandArrow) : ImageSourceHelper.FromName(Icon.CollapseArrow);
                    });
                    break;
                case SortByMode.LastEdited:
                    break;
                case SortByMode.LastCreated:
                    MainThread.BeginInvokeOnMainThread(() => {
                        ToggleSortByStatusTBI.Opacity = lowVisibilityAlpha;
                        ToggleSortByNameTBI.Opacity = lowVisibilityAlpha;
                        ToggleSortByOwnerTBI.Opacity = lowVisibilityAlpha;
                        ToggleSortByCreatedTBI.Opacity = highVisibilityAlpha;
                        ToggleSortByLabelsTBI.Opacity = lowVisibilityAlpha;
                        
                        ToggleSortByCreatedTBI.ImageSource = !descending ? ImageSourceHelper.FromName(Icon.ExpandArrow) : ImageSourceHelper.FromName(Icon.CollapseArrow);
                    });
                    break;
                case SortByMode.Label:
                    MainThread.BeginInvokeOnMainThread(() => {
                        ToggleSortByStatusTBI.Opacity = lowVisibilityAlpha;
                        ToggleSortByNameTBI.Opacity = lowVisibilityAlpha;
                        ToggleSortByOwnerTBI.Opacity = lowVisibilityAlpha;
                        ToggleSortByCreatedTBI.Opacity = lowVisibilityAlpha;
                        ToggleSortByLabelsTBI.Opacity = highVisibilityAlpha;
                        
                        ToggleSortByLabelsTBI.ImageSource = !descending ? ImageSourceHelper.FromName(Icon.ExpandArrow) : ImageSourceHelper.FromName(Icon.CollapseArrow);
                    });
                    break;
                case SortByMode.Owner:
                    MainThread.BeginInvokeOnMainThread(() => {
                        ToggleSortByStatusTBI.Opacity = lowVisibilityAlpha;
                        ToggleSortByNameTBI.Opacity = lowVisibilityAlpha;
                        ToggleSortByOwnerTBI.Opacity = highVisibilityAlpha;
                        ToggleSortByCreatedTBI.Opacity = lowVisibilityAlpha;
                        ToggleSortByLabelsTBI.Opacity = lowVisibilityAlpha;
                        
                        ToggleSortByOwnerTBI.ImageSource = !descending ? ImageSourceHelper.FromName(Icon.ExpandArrow) : ImageSourceHelper.FromName(Icon.CollapseArrow);
                    });
                    break;
                case SortByMode.Status:
                    MainThread.BeginInvokeOnMainThread(() => {
                        ToggleSortByStatusTBI.Opacity = highVisibilityAlpha;
                        ToggleSortByNameTBI.Opacity = lowVisibilityAlpha;
                        ToggleSortByOwnerTBI.Opacity = lowVisibilityAlpha;
                        ToggleSortByCreatedTBI.Opacity = lowVisibilityAlpha;
                        ToggleSortByLabelsTBI.Opacity = lowVisibilityAlpha;
                        
                        ToggleSortByStatusTBI.ImageSource = !descending ? ImageSourceHelper.FromName(Icon.ExpandArrow) : ImageSourceHelper.FromName(Icon.CollapseArrow);
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SelectAll() => ItemGrid.SelectAll();

        private FileGridMode? _lastWrapSizeChangedMode;
        private void OnWrapViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args) {
            if (!(sender is ItemsWrapGrid iwg)) return;
            switch (Mode) {
                case FileGridMode.Grid:
                    iwg.ItemWidth = double.NaN;
                    if (_lastWrapSizeChangedMode != Mode) {
                        iwg.MaximumRowsOrColumns = -1;
                        ItemGrid.ItemTemplate = GridModeTemplate;
                        _lastWrapSizeChangedMode = Mode;
                    }

                    break;
                case FileGridMode.List:
                    iwg.ItemWidth = args.EffectiveViewport.Width;
                    if (_lastWrapSizeChangedMode != Mode) {
                        iwg.MaximumRowsOrColumns = 1;
                        // ItemGrid.ItemTemplate = ListModeTemplate;
                        _lastWrapSizeChangedMode = Mode;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs args) {
            CustomDropdown.CloseDropdown();

            foreach (var addedItem in args.AddedItems) {
                var item = ItemGrid.ContainerFromItem(addedItem);

                if (!(item is GridViewItem selectedItem) || !(selectedItem.ContentTemplateRoot is Grid grid)) continue;
                ((ContentPresenter) VisualTreeHelper.GetParent(grid)).Background = ColorCreator.FromHex("#E8F0FE").ToBrush();

                var nameLabel = (TextBlock) grid.FindName("ContentName");
                if (nameLabel != null) nameLabel.Foreground = Colors.Navy.ToBrush();
            }

            foreach (var addedItem in args.RemovedItems) {
                var item = ItemGrid.ContainerFromItem(addedItem);

                if (!(item is GridViewItem selectedItem) || !(selectedItem.ContentTemplateRoot is Grid grid)) continue;
                var parent = (ContentPresenter) VisualTreeHelper.GetParent(grid);
                if (parent != null) {
                    parent.Background = Colors.Transparent.ToBrush();
                }

                var nameLabel = (TextBlock) grid.FindName("ContentName");
                if (nameLabel != null) nameLabel.Foreground = Configuration.Theme.Text;
            }
        }

        private void ShowDropdown(DependencyObject container, IFMSListableModel model, Point? position = null, FrameworkElement anchor = null) {
            if (position == null && anchor == null) throw new ArgumentNullException();
            if (position != null && anchor != null) throw new ArgumentException();
            
            Logger.Log("[CustomFileGridView]", "ShowDropdown", model);

            List<CustomDropdownViewTemplate> dropdownList = null;
            switch (model) {
                case FileModel fileModel:
                    dropdownList = new List<CustomDropdownViewTemplate> {
                        new CustomDropdownItem(ResourceHelper.GetString("open"), () => OpenFile(fileModel)),
                        new CustomDropdownItem(ResourceHelper.GetString("rename"), () => OpenFileRenamePopup(fileModel)),
                        new CustomDropdownItem(ResourceHelper.GetString("copy_move"), () => OpenCopyMovePopup(fileModel)),
                        new CustomDropdownItem(ResourceHelper.GetString("favorite"), () => ToggleFavorite(fileModel, container)),
                        new CustomDropdownItem(ResourceHelper.GetString("export"), () => Export(fileModel)),
                        new CustomDropdownItem(ResourceHelper.GetString("delete"), () => Delete(fileModel)),
                    };
                    break;
                case DirectoryModel directoryModel:
                    dropdownList = new List<CustomDropdownViewTemplate> {
                        new CustomDropdownItem(ResourceHelper.GetString("open"), () => App.FileManagerScreen.NavigateToDirectory(directoryModel)),
                        new CustomDropdownItem(ResourceHelper.GetString("rename"), () => OpenDirectoryRenamePopup(directoryModel)),
                    };
                    break;
            }

            if (dropdownList == null) return;
            if (position != null) {
                CustomDropdown.ShowDropdown(dropdownList, (Point) position);
            } else {
                CustomDropdown.ShowDropdown(dropdownList, anchor);
            }
        }

        private void OnItemContextRequested(UIElement sender, ContextRequestedEventArgs args) {
            args.TryGetPosition(Window.Current.Content, out var p);
            var container = GetParent(sender, 3);
            var correspondingModel = (IFMSListableModel) ItemGrid.ItemFromContainer(container);
            ShowDropdown(container, correspondingModel, p);
        }

        private void OnItemDoubleTapped(object sender, DoubleTappedRoutedEventArgs args) {
            switch (ItemGrid.SelectedItem) {
                case FileModel fm:
                    args.Handled = true;
                    OpenFile(fm);
                    break;
                case DirectoryModel dm:
                    args.Handled = true;
                    OpenDirectory(dm);
                    break;
            }

        }

        private DependencyObject GetParent(DependencyObject obj) => VisualTreeHelper.GetParent(obj);
        private DependencyObject GetParent(DependencyObject obj, int idx) {
            for (var i = 0; i < idx; i++) {
                obj = GetParent(obj);
            }

            return obj;
        }

        private void OnItemMenuPressed(object s, EventArgs _) {
            Logger.Log("[CustomFileGridView]", "OnItemMenuPressed", s);
            var sender = (FrameworkElement) s;
            DependencyObject container;
            switch (Mode) {
                case FileGridMode.Grid:
                    // container = GetParent(sender, 5);
                    container = null;
                    break;
                case FileGridMode.List:
                    container = sender.GetParent().GetParent().GetParent().GetParent().GetParent();
                    Logger.Log("Parent", sender.GetParent().GetParent().GetParent().GetParent().GetParent());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Mode));
            }
            
            var correspondingModel = (IFMSListableModel) ItemGrid.ItemFromContainer(container);
            ShowDropdown(container, correspondingModel, anchor: sender);
        }

        public void OpenDirectory(DirectoryModel directoryModel) {
            App.FileManagerScreen.NavigateToDirectory(directoryModel);
        }

        // dropdown items
        private async void Export(FileModel reducedFileModel) {
            CustomDropdown.CloseDropdown();
            var fullFileModel = (await FileHelper.GetFileAsync(reducedFileModel.Id));

            var ainoteExtensions = new List<string> {".ainote", ".ainotes"};
            var pdfExtensions = new List<string> {".pdf"};
            var f = new FileSavePicker {
                SuggestedFileName = fullFileModel.Name,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                DefaultFileExtension = ".ainote",
                FileTypeChoices = {
                    {"AINotes File", ainoteExtensions},
                    {"PDF File", pdfExtensions}
                },
                CommitButtonText = "Export"
            };

            var selectedFile = await f.PickSaveFileAsync();
            if (selectedFile == null) return;
            
            if (ainoteExtensions.Contains(selectedFile.FileType)) {
                Logger.Log("[CustomFileGridView]", "Export: AINotes File");
                CachedFileManager.DeferUpdates(selectedFile);
                await FileIO.WriteTextAsync(selectedFile, await FileHelper.GetFileJsonAsync(fullFileModel));
                var status = await CachedFileManager.CompleteUpdatesAsync(selectedFile);
                App.Page.Notifications.Add(new MDNotification(status == Windows.Storage.Provider.FileUpdateStatus.Complete ? "Export successful" : "Export failed"));
            } else if (pdfExtensions.Contains(selectedFile.FileType)) {
                Logger.Log("[CustomFileGridView]", "Export: PDF File");
                MainThread.BeginInvokeOnMainThread(async () => {
                    var pageCanvas = new InkCanvas {Width = 200, Height = 200, Visibility = Visibility.Collapsed};
                    App.Page.AbsoluteOverlay.AddChild(pageCanvas, new RectangleD(30, 30, 200, 200));
                    var strokeStream = new MemoryStream(fullFileModel.StrokeContent.Deserialize<byte[]>()).AsRandomAccessStream();
                    await pageCanvas.InkPresenter.StrokeContainer.LoadAsync(strokeStream);
                    var backgroundImagePos = (pageCanvas.InkPresenter.StrokeContainer.BoundingRect.X, pageCanvas.InkPresenter.StrokeContainer.BoundingRect.Y);
                    
                    var file = await ApplicationData.Current.LocalFolder.CreateFileAsync("temp-bg.gif", CreationCollisionOption.GenerateUniqueName);
                    var backgroundImagePath = file.Path;
                    var fs = await file.OpenStreamForWriteAsync();
                    await pageCanvas.InkPresenter.StrokeContainer.SaveAsync(fs.AsOutputStream(), InkPersistenceFormat.GifWithEmbeddedIsf);
                    fs.Close();
                    fs.Dispose();
                    App.Page.AbsoluteOverlay.Children.Remove(pageCanvas);
                    var result = await App.SendToAppService(new ValueSet {{"json2pdf", (await FileHelper.GetFileJsonAsync(fullFileModel), backgroundImagePath, backgroundImagePos.Serialize(), selectedFile.Path).Serialize()}});
                    Logger.Log("Print:", result.Message.Keys.ToFString(), result.Message.Values.ToFString());
                    App.Page.Notifications.Add(new MDNotification(result.Message.Values.Contains("ok") ? "Export successful" : "Export failed"));
                });
            } else {
                App.Page.Notifications.Add(new MDNotification("Export failed"));
            }
        }

        public void OpenFile(FileModel fileModel) {
            CustomDropdown.CloseDropdown();
            MainThread.BeginInvokeOnMainThread(() => App.Page.Title = fileModel.Name);
            
            if (App.Page.Content != App.EditorScreen) {
                App.Page.Load(App.EditorScreen);
            }

            App.EditorScreen.LoadFile(fileModel.FileId, true);
        }

        private async void ToggleFavorite(IFMSListableModel model, DependencyObject container) {
            CustomDropdown.CloseDropdown();
            switch (model) {
                case FileModel fileModel:
                    var newValue = !fileModel.IsFavorite;

                    await FileHelper.SetFavorite(fileModel, newValue);
                    var children = ((FrameworkElement) container).ListChildren();
                    foreach (var itm in children) {
                        if (!(itm is FileLabelView fileLabelView)) continue;
                        fileLabelView.IsFavorite = newValue;
                        break;
                    }

                    ModelCollection.First(m => m is FileModel fm && fm.Id == fileModel.FileId).IsFavorite = newValue;
                    break;
            }
        }

        private async void OpenCopyMovePopup(FileModel fileModel) {
            var currentDirectory = App.FileManagerScreen.CurrentDirectory;
            var directoryList = new CustomFileGridView {
                Width = 800,
                Height = 500,
                Mode = FileGridMode.List,
            };
            directoryList.ModelCollection.AddRange(await FileHelper.ListDirectoriesAsync(currentDirectory.DirectoryId));

            MDButton moveButton = null;
            moveButton = new MDButton {
                ButtonStyle = MDButtonStyle.Primary,
                Text = ResourceHelper.GetString("move"),
                Command = async () => {
                    var directoryId = (directoryList.SelectedModels.FirstOrDefault(m => m is DirectoryModel) as DirectoryModel)?.DirectoryId;
                    if (!(directoryId is { } pId)) {
                        if (moveButton != null) moveButton.ButtonStyle = MDButtonStyle.Error;
                        return;
                    }

                    if (moveButton != null) moveButton.ButtonStyle = MDButtonStyle.Primary;

                    await FileHelper.UpdateFileAsync(fileModel.FileId, parentDirectoryId: pId);
                    App.FileManagerScreen.LoadFiles();
                    MDPopup.CloseCurrentPopup();
                },
            };

            MDButton copyButton = null;
            copyButton = new MDButton {
                ButtonStyle = MDButtonStyle.Primary,
                Text = ResourceHelper.GetString("copy"),
                Command = async () => {
                    var directoryId = (directoryList.SelectedModels.FirstOrDefault(m => m is DirectoryModel) as DirectoryModel)?.DirectoryId;
                    if (!(directoryId is { } pId)) {
                        if (copyButton != null) copyButton.ButtonStyle = MDButtonStyle.Error;
                        return;
                    }
                    if (copyButton != null) copyButton.ButtonStyle = MDButtonStyle.Primary;

                    var newFileId = await FileHelper.CreateFileAsync(fileModel.Name, fileModel.Subject, pId);
                    await FileHelper.UpdateFileAsync(newFileId, strokeContent: fileModel.StrokeContent);
                    var currentComponentModels = await fileModel.GetComponentModels();
                    foreach (var componentModel in currentComponentModels) {
                        await FileHelper.CreateComponentAsync(new ComponentModel {
                            FileId = newFileId,
                            Type = componentModel.Type,
                            Content = componentModel.Content,
                            Position = componentModel.Position,
                            Size = componentModel.Size,
                            Deleted = componentModel.Deleted
                        });
                    }

                    MDPopup.CloseCurrentPopup();
                },
            };

            var popup = new MDContentPopup(ResourceHelper.GetString("copy_move"), new Frame {
                Background = Configuration.Theme.Background,
                Content = new StackPanel {
                    Children = {
                        directoryList
                    }
                }
            }, submitable: false, cancelable: true, cancelCallback: MDPopup.CloseCurrentPopup, buttons: new UIElement[] {
                moveButton,
                copyButton
            });

            PopupNavigation.OpenPopup(popup);
        }

        private async void Delete(FileModel fileModel) {
            CustomDropdown.CloseDropdown();
            await FileHelper.DeleteFileAsync(fileModel);
        }

        public static void OpenDirectoryRenamePopup(DirectoryModel directoryModel) {
            CustomDropdown.CloseDropdown();
            Logger.Log("OpenDirectoryRenamePopup");

            var directoryNameEntry = new MDEntry {
                Placeholder = "Ordnername",
                Text = directoryModel.Name,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 150
            };

            var cancelButton = new MDButton {
                ButtonStyle = MDButtonStyle.Secondary,
                Text = ResourceHelper.GetString("cancel"),
                HorizontalAlignment = HorizontalAlignment.Right,
                Command = PopupNavigation.CloseCurrentPopup
            };
            var submitButton = new MDButton {
                Text = ResourceHelper.GetString("ok"),
                HorizontalAlignment = HorizontalAlignment.Right,
                Command = async () => {
                    await FileHelper.UpdateDirectoryAsync(directoryModel.DirectoryId, directoryNameEntry.Text);
                    PopupNavigation.CloseCurrentPopup();
                }
            };

            new MDPopup {
                Title = "Ordner umbenennen",
                Content = new Frame {
                    Background = Configuration.Theme.Background,
                    Margin = new Thickness(15),
                    Content = new StackPanel {
                        Children = {
                            new Frame {Height = 10},
                            directoryNameEntry,
                            new Frame {Height = 3},
                            new StackPanel {
                                Orientation = Orientation.Horizontal,
                                HorizontalAlignment = HorizontalAlignment.Right,
                                Children = {
                                    cancelButton,
                                    new Frame {Width = 3},
                                    submitButton,
                                },
                            },
                        }
                    },
                }
            }.Show();
            directoryNameEntry.Focus(FocusState.Programmatic);
        }

        public static async void OpenFileRenamePopup(FileModel fileModel) {
            CustomDropdown.CloseDropdown();
            Logger.Log("OpenFileRenamePopup");

            var fileNameEntry = new MDEntry {
                Placeholder = ResourceHelper.GetString("file_name"),
                Text = fileModel.Name,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 150
            };

            var cancelButton = new MDButton {
                ButtonStyle = MDButtonStyle.Secondary,
                Text = ResourceHelper.GetString("cancel"),
                HorizontalAlignment = HorizontalAlignment.Right,
                Command = PopupNavigation.CloseCurrentPopup
            };
            var labelOptionsDict = new Dictionary<CheckBox, LabelModel>();
            var submitButton = new MDButton {
                Text = ResourceHelper.GetString("ok"),
                HorizontalAlignment = HorizontalAlignment.Right,
                Command = async () => {
                    var selectedLabelIds = labelOptionsDict.Where(kv => kv.Key.IsChecked ?? false).Select(kv => kv.Value.LabelId);

                    await FileHelper.UpdateFileAsync(fileModel.FileId, fileNameEntry.Text, "none", labelIds: selectedLabelIds.ToList());
                    PopupNavigation.CloseCurrentPopup();
                }
            };

            var labelOptionsContent = new StackPanel {
                Orientation = Orientation.Vertical
            };
            var labelOptions = new ScrollViewer {
                Content = labelOptionsContent
            };
            var labels = await FileHelper.ListLabelsAsync();
            labels.Reverse();
            var itr = 0;
            StackPanel currentStack = null;
            foreach (var label in labels) {
                // Logger.Log(itr, "=>", itr % 3);
                if (itr % 3 == 0) {
                    currentStack = new StackPanel {
                        Orientation = Orientation.Horizontal
                    };
                    labelOptionsContent.Children.Add(currentStack);
                }

                // Logger.Log(itr, label.Name);
                var checkBox = new CheckBox {
                    Content = label.Name,
                    Background = label.Color.ToBrush(),
                    IsChecked = fileModel.Labels.Contains(label.LabelId)
                };
                currentStack?.Children.Add(checkBox);
                labelOptionsDict.Add(checkBox, label);
                itr++;
            }

            Grid g = null;
            labelOptionsContent.SizeChanged += (_, args) => {
                Logger.Log("SizeChanged");
                if (g == null) return;
                g.ColumnDefinitions[0].Width = new GridLength(args.NewSize.Width);
            };

            new MDPopup {
                Title = ResourceHelper.GetString("file_create"),
                Content = new Frame {
                    Background = Configuration.Theme.Background,
                    Margin = new Thickness(15),
                    Content = g = new Grid {
                        RowDefinitions = {
                            new RowDefinition {Height = new GridLength(1, GridUnitType.Auto)},
                            new RowDefinition {Height = new GridLength(1, GridUnitType.Star)},
                            new RowDefinition {Height = new GridLength(1, GridUnitType.Auto)},
                        },
                        ColumnDefinitions = {
                            new ColumnDefinition {Width = new GridLength(1)},
                            new ColumnDefinition {Width = new GridLength(1, GridUnitType.Auto)},
                        },
                        ColumnSpacing = 10,
                        Children = {
                            {
                                new StackPanel {
                                    Children = {
                                        new MDLabel {Text = "File Options"},
                                        new Frame {Height = 10},
                                        fileNameEntry,
                                        new Frame {Height = 3},
                                    }
                                },
                                1, 0
                            }, {
                                new StackPanel {
                                    Orientation = Orientation.Vertical,
                                    Children = {
                                        new MDLabel {Text = "Labels"},
                                        new Frame {Height = 10},
                                        labelOptions,
                                        new Frame {Height = 25},
                                    }
                                },
                                1, 1
                            }, {
                                new StackPanel {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalAlignment = HorizontalAlignment.Right,
                                    Children = {
                                        cancelButton,
                                        new Frame {Width = 3},
                                        submitButton,
                                    },
                                },
                                2, 1
                            },
                        }
                    }
                }
            }.Show();
            fileNameEntry.Focus(FocusState.Programmatic);
        }
    }
}