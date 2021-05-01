using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using AINotes.Controls.Pages;
using AINotes.Helpers;
using AINotes.Helpers.Extensions;
using AINotes.Helpers.Imaging;
using AINotes.Helpers.Merging;
using Helpers;
using Helpers.Essentials;
using Helpers.Extensions;
using AINotes.Models;
using MaterialComponents;
using SortByMode = AINotes.Controls.FileManagement.SortByMode;

namespace AINotes.Screens {
    public partial class FileManagerScreen {
        private const string Context = "Screens_FileManagerScreen";
        
        private readonly MDToolbarItem _profilePictureTBI = new MDToolbarItem {
            ImageSource = new BitmapImage(new Uri(SavedStatePreferenceHelper.Get("lastDisplayedProfilePicture", Configuration.DefaultProfilePicture))),
            AutomationName = "ProfilePictureMenuTBI",
            ToolTip = "Menu",
            Padding = new Thickness(-1, 0, -1, 0),
            Margin = new Thickness(0, 6, 6, 6),
        };

        private readonly MDToolbarItem _createFolderTBI = new MDToolbarItem {
            ImageSource = new BitmapImage(new Uri(Icon.AddFolder)),
            AutomationName = "AddFolderTBI",
            ToolTip = "Create Folder"
        };

        private readonly MDToolbarItem _createFileTBI = new MDToolbarItem {
            ImageSource = new BitmapImage(new Uri(Icon.AddFile)),
            AutomationName = "AddFileTBI",
            ToolTip = "Create File"
        };
        
        public FileManagerScreen() {
            Logger.Log("[FileManagerScreen]", "-> Constructor", logLevel: LogLevel.Debug);
            InitializeComponent();
            
            InitializeComponentProperties();
            
            NavigateToDirectory(null);

            FileHelper.FileChanged += UpdateFiles;
            FileHelper.DirectoryChanged += UpdateDirectories;

            FileMerger.ApprovalRequested += OnRemoteFileApprovalRequested;

            Logger.Log("[FileManagerScreen]", "<- Constructor", logLevel: LogLevel.Debug);
        }

        // update the file list when updated
        private void UpdateFiles(FileModel model, ChangeType changeType) => LoadFiles();
        private void UpdateDirectories(DirectoryModel model, ChangeType changeType) => LoadDirectories();
        
        // update the profile picture
        private void OnAccountChanged() {
            if (App.Page.Content != this) return;
            if (App.Page.PrimaryToolbarChildren.Count == 0) return;
            
            void OnCloudItemClicked(object sender, EventArgs eventArgs) {
                if (CloudAdapter.IsLoggedIn) {
                    OpenAccountOptionsDropdown(sender);
                } else {
                    OpenCloudLoginPopup();
                }
            }

            var profileToolbarItem = (MDToolbarItem) App.Page.PrimaryToolbarChildren[1];
            var cloudStatusToolbarItem = (MDToolbarItem) App.Page.PrimaryToolbarChildren[3];

            cloudStatusToolbarItem.Pressed -= OnCloudItemClicked;   
            cloudStatusToolbarItem.Pressed += OnCloudItemClicked;

            if (CloudAdapter.IsLoggedIn) {
                // update the picture
                profileToolbarItem.SetProfilePicture(CloudAdapter.CurrentRemoteUserModel.RemoteId);
                // update the status icon
                cloudStatusToolbarItem.ImageSource = ImageSourceHelper.FromName(Icon.CloudOk);
            } else {
                // update the status icon
                cloudStatusToolbarItem.ImageSource = ImageSourceHelper.FromName(Icon.CloudError);
            }
        }

        private void OpenAccountOptionsDropdown(object sender) {
            CustomDropdown.ShowDropdown(new List<CustomDropdownViewTemplate> {
                new CustomDropdownItem("Account", OpenManageAccountPopup, Icon.Account),
                new CustomDropdownItem("Invitations", OpenManageInvitationsPopup, FileMerger.Invitations.Count > 0 ? Icon.IncomingMessage : Icon.EmptyMailingBox),
                new CustomDropdownItem("Reconnect", CloudAdapter.Restart, Icon.Reload),
                new CustomDropdownItem("Logout", CloudAdapter.Logout, Icon.Exit),
                new CustomDropdownItem("Change Account", OpenCloudLoginPopup, Icon.AccountGroup),
            }, sender as MDToolbarItem);
        }

        private void OnRemoteFileApprovalRequested() {
            MainThread.BeginInvokeOnMainThread(() => {
                var cloudStatusToolbarItem = (MDToolbarItem) App.Page.PrimaryToolbarChildren[3];

                if (FileMerger.Invitations.Count > 0) {
                    cloudStatusToolbarItem.ImageSource = Preferences.UseAnimatedIcons ? ImageSourceHelper.FromName(Icon.MailboxAnimated) : ImageSourceHelper.FromName(Icon.Mailbox);
                } else {
                    cloudStatusToolbarItem.ImageSource = ImageSourceHelper.FromName(Icon.CloudOk);
                    cloudStatusToolbarItem.BorderBrush = null;
                }
            });
        }
        
        // load content properties
        private async void InitializeComponentProperties() {
            Logger.Log("[FileManagerScreen]", "-> LoadContentProperties", logLevel: LogLevel.Verbose);

            // load saved state
            // DirectoryContainer.DoubleTappedCallback = dm => App.FileManagerScreen.NavigateToDirectory(dm);
            // DirectoryContainer.Mode = (FileGridMode) SavedStatePreferenceHelper.Get("DirectoryContainerFileGridMode", (int) FileGridMode.Grid);
            
            // FileContainer.Mode = FileGridMode.List; // (FileGridMode) SavedStatePreferenceHelper.Get("FileContainerFileGridMode", (int) FileGridMode.List);
            var sortByMode = (SortByMode) SavedStatePreferenceHelper.Get("FileContainerFileSortByMode", (int) SortByMode.LastCreated);
            var sortByModeDescending = SavedStatePreferenceHelper.Get("FileContainerFileSortByModeDescending", false);
            Logger.Log("[FileManagerScreen]", "LoadContentProperties: SavedStateSortBy", FileContainer.SortByMode, "|", FileContainer.SortByModeDescending);

            FileContainer.SortBy(sortByMode, sortByModeDescending);
            
            _createFileTBI.Pressed += (_, _) => OpenFileCreationPopup(CurrentDirectory.DirectoryId);
            _createFolderTBI.Pressed += (_, _) => OpenFolderCreationPopup(CurrentDirectory.DirectoryId);
            
            _profilePictureTBI.Pressed += OnProfilePictureTBIPressed;
            
            // set custom label filters
            await LoadFilterChips();
            ResetFilterTBI.Pressed += OnResetFilterTBIPressed;
            ResetFilterTBI.ToolTip = "Reset Filter";
            
            FilterChips.Width = (ActualWidth - (24 + 32)).Clamp(0, double.MaxValue);
            SizeChanged += (_, _) => (FilterChips.Width = ActualWidth - (24 + 32)).Clamp(0, double.MaxValue);
            
            Preferences.CustomLabels.LabelListChanged += async () => await LoadFilterChips();
            
            Logger.Log("[FileManagerScreen]", "<- LoadContentProperties", logLevel: LogLevel.Verbose);
        }

        private void OnProfilePictureTBIPressed(object s, EventArgs e) {
            CustomDropdown.ShowDropdown(new List<CustomDropdownViewTemplate> {
                new CustomDropdownItem("Settings", () => {
                    CustomDropdown.CloseDropdown();
                    if (App.Page.Content == App.SettingsScreen) return;
                    App.Page.Load(App.SettingsScreen);
                }, Icon.Settings),
                new CustomDropdownItem("Feedback", () => {
                    CustomDropdown.CloseDropdown();
                    if (App.Page.Content == App.FeedbackScreen) return;
                    App.Page.Load(App.FeedbackScreen);
                }, Icon.Feedback),
                new CustomDropdownItem("About", () => {
                    CustomDropdown.CloseDropdown();
                    if (App.Page.Content == App.AboutScreen) return;
                    App.Page.Load(App.AboutScreen);
                }, Icon.Help),
                new CustomDropdownItem("ImportFromOneNote", () => {
                    CustomDropdown.CloseDropdown();
                    OpenImportPopup(CurrentDirectory.DirectoryId);
                }, Icon.DatabaseImport),
                new CustomDropdownItem("AppInfo", () => {
                    CustomDropdown.CloseDropdown();
                    OpenAppInfoPopup();
                }, Icon.Info),
                new CustomDropdownItem("Profile", () => {
                    CustomDropdown.CloseDropdown();
                    if (CloudAdapter.IsLoggedIn) {
                        OpenManageAccountPopup();
                    } else {
                        OpenCloudLoginPopup();
                    }
                }, Icon.Account),
            }, _profilePictureTBI);
        }

        private async Task LoadFilterChips() {
            FilterChips.Children.Clear();

            var customLabels = (await FileHelper.ListLabelsAsync()).Where(lbl => !lbl.Archived).ToList();
            if (CurrentFilterLabels != null) {
                foreach (var flId in CurrentFilterLabels.ToArray()) {
                    if (customLabels.Any(itm => itm.LabelId == flId)) continue;
                    CurrentFilterLabels.Remove(flId);
                }
            }
            var optionDict = new Dictionary<MDFilterChip, int>();
                
            void OnFilterChipSelectionChanged() {
                var selectedLabelIds = new List<int>();
                foreach (var itm in FilterChips.Children) {
                    if (itm is MDFilterChip rb && rb.IsSelected) {
                        selectedLabelIds.Add(optionDict[rb]);
                    }
                }

                CurrentFilterLabels = selectedLabelIds;
                if (selectedLabelIds.Count > 0) {
                    _currentFilterCommand = dictionaryTuple => {
                        var (oldFiles, oldDirectories) = dictionaryTuple;

                        var newFiles = new Dictionary<int, FileModel>();
                        foreach (var (key, value) in oldFiles.Where(itm => selectedLabelIds.ContainsAny(itm.Value.Labels))) {
                            newFiles.Add(key, value);
                        }

                        return (newFiles, oldDirectories);
                    };
                } else {
                    _currentFilterCommand = null;
                }

                LoadFiles();
            }
            
            foreach (var customLabel in customLabels) {
                var filterChip = new MDFilterChip {
                    Text = customLabel.Name,
                    ColorBrush = customLabel.Color.ToBrush(),
                    IsSelected = CurrentFilterLabels?.Contains(customLabel.LabelId) ?? false
                };
                optionDict.Add(filterChip, customLabel.LabelId);
                FilterChips.Children.Add(filterChip);
                
                filterChip.Selected += OnFilterChipSelectionChanged;
                filterChip.Deselected += OnFilterChipSelectionChanged;
            }

            OnFilterChipSelectionChanged();
        }
        
        private IEnumerable<UIElement> _primaryToolbarItems;
        public IEnumerable<UIElement> PrimaryToolbarItems => _primaryToolbarItems ??= new List<UIElement> {
            new Frame {Width = 6},
            _profilePictureTBI,
            new Frame {Width = 12},
            new MDToolbarItem {
                ImageSource = new BitmapImage(new Uri(Icon.CloudDashed)),
                AutomationName = "CloudMenuTBI",
                ToolTip = "ConnectCloud"
            },
            new Frame { Width = 12 },
            _createFolderTBI,
            _createFileTBI,
        };
        
        // navigate to the given directory
        public void NavigateToDirectory(DirectoryModel directory) {
            Logger.Log("[FileManagerScreen]", $"-> NavigateToDirectory {directory?.Name ?? "null"}{(directory == null ? "" : " (" + directory.DirectoryId + ")")}", logLevel: LogLevel.Debug);
            
            // update the back button
            if (directory == null || directory.DirectoryId == 0) {
                App.Page.OnBackPressed = null;
            } else {
                App.Page.OnBackPressed = async () => {
                    var parentDirectory = await FileHelper.GetDirectoryAsync(directory.ParentDirectoryId);
                    NavigateToDirectory(parentDirectory);
                };
            }

            // set directory
            CurrentDirectory = directory;
            
            // load files & directories
            LoadFiles();
            LoadDirectories();
            
            // set the title
            App.Page.Title = "FileManager" + (Preferences.ShowPathInTitle && CurrentDirectory.DirectoryId != 0 ? ": " + CurrentDirectory.Name : "");
        }

        private bool _firstLoad = true;

        public override void OnLoad() {
            Logger.Log("[FileManagerScreen]", "-> OnLoad", logLevel: LogLevel.Verbose);
            base.OnLoad();

            // load all toolbar items
            Logger.Log("[FileManagerScreen]", "OnLoad -> Load ToolbarItems", logLevel: LogLevel.Verbose);
            App.Page.PrimaryToolbarChildren.Clear();
            App.Page.SecondaryToolbarChildren.Clear();
            
            foreach (var primaryItem in PrimaryToolbarItems) {
                App.Page.PrimaryToolbarChildren.Add(primaryItem);
            }
            Logger.Log("[FileManagerScreen]", "OnLoad <- Load ToolbarItems", logLevel: LogLevel.Verbose);

            // set title & profile picture
            App.Page.Title = "FileManager";
            
            OnAccountChanged();
            CloudAdapter.AccountChanged += () => MainThread.BeginInvokeOnMainThread(OnAccountChanged);

            // back button action
            Action backButtonAction = null;
            if (CurrentDirectory != null && CurrentDirectory.DirectoryId != 0) {
                backButtonAction = async () => NavigateToDirectory(await FileHelper.GetDirectoryAsync(CurrentDirectory.ParentDirectoryId));
            }

            App.Page.OnBackPressed = backButtonAction;

            // load shortcuts & changelog on first load
            if (_firstLoad) {
                _firstLoad = false;
                LoadShortcuts();

                Task.Run(async () => {
                    if (await VersionTracking.GetUpdatesAvailable()) {
                        MainThread.BeginInvokeOnMainThread(() => {
                            #if !DEBUG
                            OpenUpdatePopup();
                            #endif
                        });
                    } else {
                        if ((VersionTracking.IsFirstLaunchForCurrentVersion || VersionTracking.IsFirstLaunchForCurrentBuild) && !VersionTracking.IsFirstLaunchEver) {
                            MainThread.BeginInvokeOnMainThread(OpenChangelogPopup);
                        }
                    }
                });
            }

            LoadFiles();
            LoadDirectories();
            
            Logger.Log("[FileManagerScreen]", "<- OnLoad", logLevel: LogLevel.Verbose);
        }

        private async void OnResetFilterTBIPressed(object sender, EventArgs e) {
            await ResetFilter();
        }
    }
}