using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AINotes.Controls;
using AINotes.Controls.Popups;
using Helpers;
using Helpers.Extensions;
using AINotes.Models;
using Newtonsoft.Json;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using AINotes.Controls.Pages;
using AINotes.Helpers;
using AINotes.Helpers.Extensions;
using AINotes.Helpers.Integrations;
using AINotes.Helpers.Merging;
using AINotesCloud;
using AINotesCloud.Models;
using Helpers.Essentials;
using Helpers.Networking;
using HtmlAgilityPack;
using MaterialComponents;
using Colors = Windows.UI.Colors;
using ColumnDefinition = Windows.UI.Xaml.Controls.ColumnDefinition;
using HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment;
using VerticalAlignment = Windows.UI.Xaml.VerticalAlignment;
using Visibility = Windows.UI.Xaml.Visibility;

namespace AINotes.Screens {
    public partial class FileManagerScreen {
        public static void OpenFolderCreationPopup(int directoryId) {
            MDPopup.CloseCurrentPopup();

            var folderNameEntry = new MDEntry {
                Placeholder = ResourceHelper.GetString(Context, "Popups_FolderCreation_FolderNameEntry"),
                Width = 300
            };

            var popup = new MDContentPopup(ResourceHelper.GetString(Context, "Popups_FolderCreation_Title"), new Frame {
                Background = Configuration.Theme.Background,
                Margin = new Thickness(15),
                Content = new StackPanel {
                    Children = {
                        folderNameEntry,
                    }
                }
            }, cancelable: true, okCallback: async () => {
                await FileHelper.CreateDirectoryAsync(string.IsNullOrWhiteSpace(folderNameEntry.Text) ? "Unnamed" : folderNameEntry.Text, directoryId);
                PopupNavigation.CloseCurrentPopup();
            });
            PopupNavigation.OpenPopup(popup);
            folderNameEntry.Focus(FocusState.Programmatic);
        }

        public static async void OpenFileCreationPopup(int directoryId) {
            MDPopup.CloseCurrentPopup();

            var fileNameEntry = new MDEntry {
                Placeholder = "Filename",
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 150
            };

            var cancelButton = new MDButton {
                ButtonStyle = MDButtonStyle.Secondary,
                Text = "Cancel",
                HorizontalAlignment = HorizontalAlignment.Right,
                Command = PopupNavigation.CloseCurrentPopup
            };
            var openFileCheckbox = new CheckBox {
                Content = "Open File",
                IsChecked = SavedStatePreferenceHelper.Get("OpenFileUponCreation", false)
            };
            var labelOptionsDict = new Dictionary<CheckBox, LabelModel>();
            var submitButton = new MDButton {
                Text = "Ok",
                HorizontalAlignment = HorizontalAlignment.Right,
                Command = async () => {
                    var name = fileNameEntry.Text;
                    var selectedLabelIds = labelOptionsDict.Where(kv => kv.Key.IsChecked ?? false).Select(kv => kv.Value.LabelId);

                    string owner = null;
                    if (SynchronizationService.CloudApi.IsLoggedIn) {
                        owner = (await SynchronizationService.CloudApi.GetUser()).RemoteId;
                    }
                    var newId = await FileHelper.CreateFileAsync(string.IsNullOrWhiteSpace(name) ? "Unnamed" : name, "none", directoryId, selectedLabelIds.ToList(), owner);
                    PopupNavigation.CloseCurrentPopup();

                    if (openFileCheckbox.IsChecked == null) return;
                    SavedStatePreferenceHelper.Set("OpenFileUponCreation", (bool) openFileCheckbox.IsChecked);

                    // ReSharper disable once InvertIf
                    if ((bool) openFileCheckbox.IsChecked) {
                        // load file
                        App.Page.Load(App.EditorScreen);
                        App.EditorScreen.LoadFile(newId);
                    }
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
                if (label.Archived) continue;
                
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
                    IsChecked = App.FileManagerScreen?.CurrentFilterLabels?.Contains(label.LabelId) ?? false
                };
                currentStack?.Children.Add(checkBox);
                labelOptionsDict.Add(checkBox, label);
                itr++;
            }

            MDPopup popup = null;
            labelOptionsContent.SizeChanged += (_, args) => {
                if (!((popup?.Content as Frame)?.Content is Grid target)) return;
                target.ColumnDefinitions[0].Width = new GridLength(args.NewSize.Width);
            };

            popup = new MDPopup {
                Content = new Frame {
                    Background = Configuration.Theme.Background,
                    Margin = new Thickness(15),
                    Content = new Grid {
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
                                new MDLabel {
                                    Text = "Create File",
                                    FontSize = 24,
                                    Margin = new Thickness(0, 0, 0, 10)
                                },
                                0, 0
                            }, {
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
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    Children = {
                                        openFileCheckbox,
                                    },
                                },
                                2, 0
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
            };
            PopupNavigation.OpenPopup(popup);
            fileNameEntry.Focus(FocusState.Programmatic);
        }

        private void OpenChangelogPopup() {
            MDPopup.CloseCurrentPopup();
            string changelogText;
            try {
                var storePage = new HtmlWeb().Load("https://www.microsoft.com/de-de/p/ainotes/9ngx8jnlllcj/");
                changelogText = storePage.DocumentNode.SelectSingleNode("//*[@id='version-notes']/p").InnerHtml;

                if (changelogText == SavedStatePreferenceHelper.Get("lastChangelog", "") || string.IsNullOrWhiteSpace(changelogText)) {
                    return;
                }

                SavedStatePreferenceHelper.Set("lastChangelog", changelogText);
            } catch (Exception ex) {
                Logger.Log("[FileManagerScreen.Popups]", "Exception in OpenChangelogPopup:", ex.ToString(), logLevel: LogLevel.Error);
                return;
            }

            new MDContentPopup("Changelog", new MDLabel(HttpUtility.HtmlDecode(changelogText))).Show();
        }

        // ReSharper disable once UnusedMember.Local
        private void OpenUpdatePopup() {
            Logger.Log("[FileManagerScreen]", "OpenUpdatePopup");
            var popup = new MDContentPopup("Update", new StackPanel {
                Children = {
                    new MDLabel {
                        Text = "New version available on the Microsoft Store!"
                    }
                }
            }, okText: "Install", okCallback: async () => {
                Logger.Log("[FileManagerScreen]", "Installing");
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://pdp/?productid=9NGX8JNLLLCJ"));
            }, closeOnOk: true, closeWhenBackgroundIsClicked: true, cancelText: "Install later", cancelable: true);

            PopupNavigation.OpenPopup(popup);
        }

        private async void OpenAppInfoPopup() {
            var versionLabel = new MDLabel("Fetching data...");

            var popup = new MDContentPopup("App Info", new StackPanel {
                Orientation = Orientation.Horizontal,
                Children = {
                    new StackPanel {
                        Children = {
                            new MDLabel {Text = "Idiom:"},
                            new MDLabel {Text = "DeviceType:"},
                            new MDLabel {Text = "NetworkAccess:"},
                            new MDLabel {Text = "Name:"},
                            new MDLabel {Text = "Manufacturer:"},
                            new MDLabel {Text = "Version:"},
                            new MDLabel {Text = "CurrentVersion:"},
                            new MDLabel {Text = "AppMode:"},
                        }
                    },
                    new Frame {Width = 10},
                    new StackPanel {
                        Children = {
                            new MDLabel {Text = DeviceInfo.Idiom.ToString()},
                            new MDLabel {Text = DeviceInfo.DeviceType.ToString()},
                            new MDLabel {Text = Connectivity.NetworkAccess.ToString()},
                            new MDLabel {Text = DeviceInfo.Name},
                            new MDLabel {Text = DeviceInfo.Manufacturer},
                            new MDLabel {Text = $"{SystemInfo.GetSystemVersion(DeviceInfo.VersionString)} (Build {DeviceInfo.VersionString})"},
                            versionLabel,
#if DEBUG
                            new MDLabel {Text = "Debug"},
#else
                            new MDLabel { Text = "Release" },
#endif
                        }
                    }
                }
            }, closeOnOk: true);
            PopupNavigation.OpenPopup(popup);

            versionLabel.Text = AppInfo.VersionString + " (checking for updates...)";
            versionLabel.Text = AppInfo.VersionString + $" ({(await VersionTracking.GetUpdatesAvailable() ? "not up to date" : "up to date")})";
        }

        private void OpenImportPopup(int directoryId) {
            MDPopup.CloseCurrentPopup();
            var cancel = false;

            var layout = new StackPanel();
            var statusLabel = new MDLabel {
                FontSize = 20,
                Text = "Import files and directories from OneNote"
            };

            var selected = false;
            var button = new MDButton {
                Text = "Import & Load"
            };
            var overview = new ScrollViewer();

            async void ButtonCommand(List<Microsoft.Graph.Notebook> notebooks = null) {
                try {
                    await OneNoteHelper.Login();
                } catch (Exception e) {
                    Logger.Log("[FileManagerScreen.Popups]", "OpenImportPopup: Login:", e.ToString());
                    return;
                }

                if (cancel) return;

                notebooks ??= await OneNoteHelper.GetNotebooks();

                if (!selected) {
                    // select
                    var selectedNotebooks = new List<Microsoft.Graph.Notebook>();
                    var overviewLayout = new StackPanel();
                    overview.Content = overviewLayout;
                    var notebookBoxes = new Dictionary<MDCheckBox, Microsoft.Graph.Notebook>();
                    foreach (var notebook in notebooks) {
                        var notebookCheckbox = new MDCheckBox {IsChecked = false};
                        var notebookItem = new Grid {
                            FlowDirection = FlowDirection.LeftToRight,
                            ColumnDefinitions = {
                                new ColumnDefinition {Width = new GridLength(40)},
                                new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)}
                            },
                            RowDefinitions = {
                                new RowDefinition {Height = new GridLength(40)}
                            },
                            Children = {
                                {notebookCheckbox, 0, 0}, {
                                    new MDLabel {
                                        VerticalAlignment = VerticalAlignment.Center,
                                        HorizontalAlignment = HorizontalAlignment.Left,
                                        Text = notebook.DisplayName
                                    },
                                    0, 1
                                }
                            }
                        };
                        notebookCheckbox.Checked += (sender, _) => { selectedNotebooks.Add(notebookBoxes[(MDCheckBox) sender]); };
                        notebookCheckbox.Unchecked += (sender, _) => { selectedNotebooks.Remove(notebookBoxes[(MDCheckBox) sender]); };
                        notebookBoxes.Add(notebookCheckbox, notebook);
                        overviewLayout.Children.Add(notebookItem);
                    }

                    layout.Children.Insert(0, overview);
                    button.Text = "Import";
                    button.Command = () => { ButtonCommand(selectedNotebooks); };
                    selected = true;
                    return;
                }

                if (cancel) return;
                layout.Children.Remove(overview);
                var notebookStatus = new MDLabel {Text = "Notebooks"};
                var notebookProgress = new ProgressBar();

                var sectionStatus = new MDLabel {Text = "Sections"};
                var sectionProgress = new ProgressBar();

                var pageStatus = new MDLabel {Text = "Pages"};
                var pageProgress = new ProgressBar();

                layout.Children.Remove(button);

                statusLabel.Text = "Import-Vorgang läuft...";

                layout.Children.Add(notebookStatus);
                layout.Children.Add(notebookProgress);

                layout.Children.Add(sectionStatus);
                layout.Children.Add(sectionProgress);

                layout.Children.Add(pageStatus);
                layout.Children.Add(pageProgress);

                var pageCanvas = new CustomInkCanvas {Width = 200, Height = 200, IsVisible = false};
                App.Page.AbsoluteOverlay.AddChild(pageCanvas, new RectangleD(30, 30, 200, 200));
                foreach (var notebook in notebooks) {
                    if (cancel) return;
                    notebookStatus.Text = $"Notebook ({notebooks.IndexOf(notebook) + 1}/{notebooks.Count}): {notebook.DisplayName}".Truncate(25, "...");
                    // notebookProgress.Progress = (double) (notebooks.IndexOf(notebook) + 1) / notebooks.Count;

                    var childDirectoryId = await FileHelper.CreateDirectoryAsync(notebook.DisplayName, directoryId);

                    var sections = await OneNoteHelper.GetSections(notebook);

                    foreach (var section in sections) {
                        if (cancel) return;
                        sectionStatus.Text = $"Section ({sections.IndexOf(section) + 1}/{sections.Count}): {section.DisplayName}".Truncate(25, "...");
                        // sectionProgress.Progress = (double) (sections.IndexOf(section) + 1) / sections.Count;

                        var pages = await OneNoteHelper.GetPages(section);

                        foreach (var page in pages) {
                            if (cancel) return;
                            pageStatus.Text = $"Page ({pages.IndexOf(page) + 1}/{pages.Count}): {page.Title}".Truncate(25, "...");
                            // pageProgress.Progress = (double) (pages.IndexOf(page) + 1) / pages.Count;

                            Logger.Log(" >", page.Title);

                            var content = await OneNoteHelper.GetPageContent(page);
                            var parsed = await OneNoteHelper.ParsePageContent(content);

                            var fileId = await FileHelper.CreateFileAsync(section.DisplayName + " - " + parsed.Title, null, childDirectoryId);

                            Logger.Log($"Loading {parsed.Components.Count} Components");
                            foreach (var p in parsed.Components) {
                                p.FileId = fileId;
                                await FileHelper.CreateComponentAsync(p);
                            }

                            Logger.Log($"Loading {parsed.Strokes.Count} Strokes");

                            foreach (var stroke in parsed.Strokes) {
                                var points = new List<StrokePointModel>();
                                foreach (var pointArray in stroke.Points) {
                                    float pressure;
                                    switch (pointArray.Length) {
                                        case 2:
                                            pressure = 0.4f;
                                            break;
                                        case 3:
                                            pressure = pointArray[2] / Configuration.OneNoteFactors.PressureFactor;
                                            break;
                                        default:
                                            throw new ArgumentOutOfRangeException();
                                    }

                                    points.Add(new StrokePointModel(pointArray[0] / Configuration.OneNoteFactors.SizeFactor, pointArray[1] / Configuration.OneNoteFactors.SizeFactor, pressure));
                                }

                                pageCanvas.AddStroke(points, stroke.Color, stroke.Width, stroke.Height, stroke.Transparency, stroke.PenTip, stroke.PenType, stroke.IgnorePressure, stroke.AntiAliased, stroke.FitToCurve);
                            }

                            var strokes = JsonConvert.SerializeObject(pageCanvas.StrokeBytes);
                            await FileHelper.UpdateFileAsync(fileId, strokeContent: strokes);

                            pageCanvas.Reset();
                        }
                    }
                }

                App.Page.AbsoluteOverlay.Children.Remove(pageCanvas);
                statusLabel.Text = "Finished!";
                Thread.Sleep(1000);
                Logger.Log("Import finished");
                MDPopup.CloseCurrentPopup();
            }

            button.Command = () => ButtonCommand();

            var popup = new MDPopup {
                Content = new Frame {
                    Background = Configuration.Theme.Background,
                    VerticalAlignment = VerticalAlignment.Center,
                    Content = new StackPanel {
                        Children = {
                            statusLabel,
                            new Frame {
                                Height = 10
                            },
                            layout,
                            new Frame {
                                Height = 10
                            },
                            new StackPanel {
                                Orientation = Orientation.Horizontal,
                                HorizontalAlignment = HorizontalAlignment.Right,
                                Spacing = 10,
                                Children = {
                                    new MDButton {
                                        ButtonStyle = MDButtonStyle.Secondary,
                                        Text = "Cancel",
                                        Command = () => {
                                            cancel = true;
                                            MDPopup.CloseCurrentPopup();
                                        }
                                    },
                                    button
                                }
                            }
                        }
                    }
                }
            };

            PopupNavigation.OpenPopup(popup);
        }

        private event Action OnAccountUpdated;

        // regex patterns

        private const string PasswordPattern = "^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$";
        private const string UsernamePattern = "^([a-zA-Z0-9_\\- ]){3,30}$";
        private const string EmailPattern = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z";

        private void OpenManageAccountPopup() {
            UIElement GetPopupContent() {
                var oldPassword = "";
                var newPassword = "";

                UIElement GetChangePasswordLayout() {
                    var changePasswordButton = new MDButton {
                        Text = "Change Password"
                    };

                    var oldPasswordEntry = new MDEntry {
                        Placeholder = "Old Password",
                        IsPasswordEntry = true
                    };
                    oldPasswordEntry.SizeChanged += (_, _) => oldPasswordEntry.Focus(FocusState.Programmatic);

                    var newPasswordEntry = new MDEntry {
                        Placeholder = "New Password",
                        IsPasswordEntry = true,
                        RegexPattern = PasswordPattern
                    };

                    var repeatNewPasswordEntry = new MDEntry {
                        Placeholder = "Repeat new Password",
                        IsPasswordEntry = true,
                        RegexPattern = PasswordPattern
                    };

                    repeatNewPasswordEntry.TextChanged += (sender, _) => ((MDEntry) sender).Error = ((MDEntry) sender).Text != newPasswordEntry.Text;

                    oldPasswordEntry.TextChanged += OnUpdateVariablesProvoked;
                    newPasswordEntry.TextChanged += OnUpdateVariablesProvoked;
                    repeatNewPasswordEntry.TextChanged += OnUpdateVariablesProvoked;

                    void OnUpdateVariablesProvoked(object _, object __) {
                        UpdateVariables();
                    }

                    void UpdateVariables() {
                        oldPassword = oldPasswordEntry.Text;
                        newPassword = newPasswordEntry.Text == repeatNewPasswordEntry.Text ? newPasswordEntry.Text : "";
                    }

                    var changePasswordStack = new StackPanel();

                    var cancelPasswordChangingButton = new MDToolbarItem(Icon.Close, (_, _) => { ChangePassword(false); });

                    changePasswordButton.Command = () => ChangePassword(true);

                    changePasswordStack = new StackPanel {
                        Spacing = 10,
                        Orientation = Orientation.Vertical,
                        Children = {
                            changePasswordButton,
                            new StackPanel {
                                Orientation = Orientation.Horizontal,
                                Children = {
                                    new StackPanel {
                                        Spacing = 10,
                                        Width = 300,
                                        Orientation = Orientation.Vertical,
                                        Children = {
                                            oldPasswordEntry,
                                            newPasswordEntry,
                                            repeatNewPasswordEntry
                                        }
                                    },
                                    cancelPasswordChangingButton
                                }
                            }
                        }
                    };

                    void ChangePassword(bool changePassword) {
                        if (changePassword) {
                            changePasswordButton.Visibility = Visibility.Collapsed;
                            ((StackPanel) changePasswordStack.Children[1]).Visibility = Visibility.Visible;
                        } else {
                            changePasswordButton.Visibility = Visibility.Visible;
                            oldPasswordEntry.Text = "";
                            newPasswordEntry.Text = "";
                            repeatNewPasswordEntry.Text = "";

                            ((StackPanel) changePasswordStack.Children[1]).Visibility = Visibility.Collapsed;
                        }
                    }

                    ChangePassword(false);

                    OnAccountUpdated += () => { ChangePassword(false); };

                    return changePasswordStack;
                }

                var profilePictureLayout = new StackPanel {
                    Padding = new Thickness(0, 0, 30, 0)
                };

                var progressIndicator = new ProgressRing {
                    Visibility = Visibility.Collapsed,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(30)
                };

                var imagePreview = new Image {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Stretch = Stretch.UniformToFill
                };

                var previewClickable = new Frame {
                    Width = 100,
                    Height = 100,
                    CornerRadius = new CornerRadius(50),
                    Padding = new Thickness(2),
                    Content = new Frame {
                        Content = imagePreview,
                        CornerRadius = new CornerRadius(48)
                    }
                };

                profilePictureLayout.Children.Add(previewClickable);
                profilePictureLayout.Children.Add(progressIndicator);

                var displayName = new MDEntry {
                    Placeholder = "Username",
                };

                var email = new MDEntry {
                    Placeholder = "Email",
                    IsEnabled = false,
                    RegexPattern = EmailPattern
                };

                async void UploadAccountData() {
                    // change password if needed
                    if (oldPassword != "" && newPassword != "" && oldPassword != newPassword) {
                        _ = await CloudAdapter.ChangePassword(oldPassword, newPassword);
                    }

                    OnAccountUpdated?.Invoke();

                    // upload personal data
                    var success = await CloudAdapter.SetUserInfo(displayName.Text);

                    if (success) {
                        FetchAccountData();
                    }
                }

                var updateAccount = new MDButton {
                    Text = "Update",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Command = UploadAccountData
                };

                var accountDataStack = new StackPanel {
                    Spacing = 10,
                    Width = 500,
                    Orientation = Orientation.Vertical,
                    Children = {
                        displayName,
                        email,
                        GetChangePasswordLayout(),
                        updateAccount
                    }
                };

                var container = new Frame {
                    Padding = new Thickness(10),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Content = new StackPanel {
                        Orientation = Orientation.Horizontal,
                        Children = {
                            profilePictureLayout,
                            accountDataStack
                        }
                    }
                };

                async void FetchAccountData() {
                    var accountModel = await CloudAdapter.GetUserInfo();

                    displayName.Text = accountModel.DisplayName;
                    email.Text = accountModel.Email;

                    imagePreview.SetProfilePicture(CloudAdapter.CurrentRemoteUserModel.RemoteId);
                }

                FetchAccountData();

                // profile picture pointer events
                previewClickable.PointerEntered += (_, _) => previewClickable.Background = new SolidColorBrush(Colors.Gray);
                previewClickable.PointerExited += (_, _) => previewClickable.Background = new SolidColorBrush(Colors.Transparent);
                previewClickable.PointerPressed += async (_, _) => {
                    var (selectedFileStream, fileName, _) = await FilePicker.PickFile(new[] {".jpeg", ".jpg", ".png", ".gif"});

                    if (fileName == null) return;
                    if (selectedFileStream == null) return;

                    progressIndicator.IsActive = true;
                    progressIndicator.Visibility = Visibility.Visible;

                    var success = await CloudAdapter.UploadProfilePicture(selectedFileStream.AsStreamForRead());

                    if (success) FetchAccountData(); else PopupNavigation.ShowError("Profile picture upload failed.");

                    progressIndicator.IsActive = false;
                    progressIndicator.Visibility = Visibility.Collapsed;
                };

                return container;
            }

            var popup = new MDContentPopup("Manage Account", new Frame {
                Background = Configuration.Theme.Background,
                Content = GetPopupContent()
            }, submitable: false);
            PopupNavigation.OpenPopup(popup);
        }

        private void OpenCloudLoginPopup() {
            static UIElement GetRegisterView() {
                var displayName = new MDEntry {
                    Placeholder = "Name",
                    RegexPattern = UsernamePattern
                };

                var email = new MDEntry {
                    Placeholder = "Email",
                    RegexPattern = EmailPattern
                };

                var password = new MDEntry {
                    Placeholder = "Password",
                    IsPasswordEntry = true,
                    RegexPattern = PasswordPattern
                };

                var registerButton = new MDButton {
                    Text = "Register",
                    Command = async () => {
                        var (success, message) = await CloudAdapter.Register(displayName.Text, email.Text, password.Text);

                        if (success) {
                            PopupNavigation.CloseCurrentPopup();
                        } else {
                            Logger.Log("[FileManagerScreen]", "Register failed: ", message);
                            PopupNavigation.ShowError(message);
                        }
                    }
                };

                var innerStack = new StackPanel {
                    Spacing = 10,
                    Orientation = Orientation.Vertical,
                    Children = {
                        displayName,
                        email,
                        password,
                        registerButton
                    }
                };

                var container = new Frame {
                    Padding = new Thickness(10),
                    Width = 300,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Content = innerStack
                };

                return container;
            }

            static UIElement GetLoginView() {
                var email = new MDEntry {
                    Placeholder = "Email",
                    RegexPattern = EmailPattern
                };

                var password = new MDEntry {
                    Placeholder = "Password",
                    IsPasswordEntry = true
                };

                var loginButton = new MDButton {
                    Text = "Login",
                    Command = async () => {
                        var success = await CloudAdapter.Login(email.Text, password.Text);

                        if (success) {
                            PopupNavigation.CloseCurrentPopup();
                        } else {
                            Logger.Log("[FileManagerScreen]", "Login failed.");
                            PopupNavigation.ShowError("Login failed.");
                        }
                    }
                };

                var innerStack = new StackPanel {
                    Spacing = 10,
                    Orientation = Orientation.Vertical,
                    Children = {
                        email,
                        password,
                        loginButton
                    }
                };

                var container = new Frame {
                    Padding = new Thickness(10),
                    Width = 300,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Content = innerStack
                };

                return container;
            }

            static UIElement GetAccountsView() {
                var savedUsersSerialized = SavedStatePreferenceHelper.Get("savedUsers", "");
                var savedUsers = savedUsersSerialized == "" ? new Dictionary<string, RemoteUserModel>() : savedUsersSerialized.Deserialize<Dictionary<string, RemoteUserModel>>();

                Logger.Log("Saved users count", savedUsers.Count);
                
                var loadingIndicator = new ProgressBar {
                    Height = 40,
                    Width = 600,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsIndeterminate = true,
                    ShowPaused = false,
                    ShowError = false
                };
                
                var container = new Frame {
                    Padding = new Thickness(10),
                    Width = 620,
                    MaxHeight = 200,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Content = loadingIndicator
                };

                Task.Run(async () => {
                    if (CloudAdapter.CurrentRemoteUserModel != null) {
                        var (key, _) = savedUsers.FirstOrDefault(pair => pair.Value.RemoteId == CloudAdapter.CurrentRemoteUserModel.RemoteId);
                        if (key != null) {
                            savedUsers.Remove(key);
                        }
                    }
                
                    var filteredUsers = new Dictionary<string, RemoteUserModel>();
                
                    foreach (var (token, remoteUserModel) in savedUsers) {
                        try { 
                            var success = await SynchronizationService.CloudApi.IsValidToken(token);
                            Logger.Log("Remote User Model", remoteUserModel.RemoteId, success);
                            if (success) filteredUsers.Add(token, remoteUserModel);
                        } catch (Exception) {
                            Logger.Log("Exception: ", remoteUserModel.RemoteId);
                        }
                    }
                
                    MainThread.BeginInvokeOnMainThread(() => {
                        var accounts = filteredUsers.Select(pair => {
                            var (token, remoteUserModel) = pair;
                            var serverUrl = Preferences.ServerUrl.ToString().EndsWith("/") ? Preferences.ServerUrl : Preferences.ServerUrl + "/";
                            var profilePictureUrl = serverUrl + @$"images/profilepicture/{pair.Value.RemoteId}";
                            return new AccountItem(remoteUserModel.DisplayName, remoteUserModel.Email, profilePictureUrl, token);
                        }).ToArray();

                        var accountsList = new MDAccountsList();
                        accountsList.SetItems(accounts.ToArray());
                
                        accountsList.OnItemSelected += async item => {
                            Logger.Log("Logging in with saved account data");
                            var (success, message) = await CloudAdapter.TokenLogin(item.Token);
                            if (success) {
                                PopupNavigation.CloseCurrentPopup();
                            } else {
                                Logger.Log("[FileManagerScreen]", "Login failed:", message);
                                PopupNavigation.ShowError("Login failed.");
                            }
                        };

                        container.Content = accountsList;
                    });
                });

                return container;
            }

            var popup = new MDContentPopup("[ALPHA] Register / Login", new Frame {
                Background = Configuration.Theme.Background,
                Content = new StackPanel {
                    Orientation = Orientation.Vertical,
                    Spacing = 20,
                    Children = {
                        GetAccountsView(),
                        new StackPanel {
                            Orientation = Orientation.Horizontal,
                            Spacing = 20,
                            Children = {
                                GetRegisterView(),
                                GetLoginView()
                            }
                        }
                    }
                }
            }, submitable: false, cancelable: true);
            
            PopupNavigation.OpenPopup(popup);
        }
        
        private void OpenManageInvitationsPopup() {
            var invitationsView = new MDInvitationsView {
                Width = 600,
                Margin = new Thickness(0, 20, 0, 20)
            };

            invitationsView.InvitationAccepted += async permissionId => {
                var success = await SynchronizationService.CloudApi.AcceptFilePermission(permissionId);
                
                if (success) {
                    if (FileMerger.Invitations.Keys.Any(permission => permission.PermissionId == permissionId)) {
                        FileMerger.Invitations.Remove(FileMerger.Invitations.Keys.FirstOrDefault(permission => permission.PermissionId == permissionId)!);
                        FileMerger.InvokeApprovalRequested();
                    }
                }
                
                Logger.Log("[FileManagerScreen.popups]", "Accept Invitation", permissionId);
            };
            
            invitationsView.InvitationDeclined += async permissionId => {
                var success = await SynchronizationService.CloudApi.DeleteFilePermission(permissionId);

                if (success) {
                    if (FileMerger.Invitations.Keys.Any(permission => permission.PermissionId == permissionId)) {
                        FileMerger.Invitations.Remove(FileMerger.Invitations.Keys.FirstOrDefault(permission => permission.PermissionId == permissionId)!);
                        FileMerger.InvokeApprovalRequested();
                    }
                }
                
                Logger.Log("[FileManagerScreen.popups]", "Declined Invitation", permissionId);
            };

            FileMerger.ApprovalRequested += () => {
                MainThread.BeginInvokeOnMainThread(() => {
                    invitationsView.SetInvitations(FileMerger.Invitations.Select(pair =>
                        new Invitation(pair.Value.Name, pair.Key.PermissionId, pair.Key.UserPermission)).ToArray());
                    if (FileMerger.Invitations.Count == 0) {
                        PopupNavigation.CloseCurrentPopup();
                        return;
                    }
                });
            };

            invitationsView.SetInvitations(FileMerger.Invitations.Select(pair => new Invitation(pair.Value.Name, pair.Key.PermissionId, pair.Key.UserPermission)).ToArray());
            
            var popup = new MDContentPopup("Manage Invitations", invitationsView,
                submitable: false, cancelable: true, cancelCallback: MDPopup.CloseCurrentPopup);
            
            PopupNavigation.OpenPopup(popup);
        }

        public static void OpenDirectoryRenamePopup(DirectoryModel directoryModel) {
            CustomDropdown.CloseDropdown();
            Logger.Log("OpenDirectoryRenamePopup");

            var directoryNameEntry = new MDEntry {
                Placeholder = "Folder name",
                Text = directoryModel.Name,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 300
            };

            directoryNameEntry.Focus(FocusState.Programmatic);

            var popup = new MDContentPopup("Rename folder", new Frame {
                Background = Configuration.Theme.Background,
                Content = directoryNameEntry
            }, cancelable: true, cancelCallback: PopupNavigation.CloseCurrentPopup, okCallback: async () => {
                await FileHelper.UpdateDirectoryAsync(directoryModel.DirectoryId, directoryNameEntry.Text);
                PopupNavigation.CloseCurrentPopup();
            });

            PopupNavigation.OpenPopup(popup);
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

            var popup = new MDPopup {
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
            };

            if (PopupNavigation.CurrentPopup == null) {
                MainThread.BeginInvokeOnMainThread(() => PopupNavigation.OpenPopup(popup));
            }

            fileNameEntry.Focus(FocusState.Programmatic);
        }
    }
}