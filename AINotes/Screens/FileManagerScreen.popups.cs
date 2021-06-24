using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using AINotes.Controls;
using AINotes.Controls.Popups;
using Helpers;
using Helpers.Extensions;
using AINotes.Models;
using Newtonsoft.Json;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AINotes.Controls.Pages;
using AINotes.Helpers;
using AINotes.Helpers.Extensions;
using AINotes.Helpers.Integrations;
using Helpers.Essentials;
using Helpers.Networking;
using HtmlAgilityPack;
using MaterialComponents;
using ColumnDefinition = Windows.UI.Xaml.Controls.ColumnDefinition;
using HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment;
using VerticalAlignment = Windows.UI.Xaml.VerticalAlignment;

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

                    // TODO: cloud integration
                    string owner = null;
                    
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

        // regex patterns

        private const string PasswordPattern = "^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$";
        private const string UsernamePattern = "^([a-zA-Z0-9_\\- ]){3,30}$";
        private const string EmailPattern = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z";

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