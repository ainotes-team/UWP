using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using AINotes.Controls.Popups;
using AINotes.Controls.Sidebar.Content;
using AINotes.Helpers;
using AINotes.Helpers.Integrations;
using AINotes.Helpers.PreferenceHelpers;
using AINotes.Helpers.Sidebar.RepresentationPlan;
using Helpers;
using Helpers.Essentials;
using Helpers.Extensions;
using AINotes.Models.Enums;
using AINotes.Screens;
using AINotesCloud;
using MaterialComponents;

namespace AINotes {
    public static class Preferences {
        // events
        public static event Action LanguageChanged;
        public static event Action ThemeChanged;
        
        // general
        public static readonly StringListPreference DisplayLanguage = new StringListPreference("Display Language", 
            () => new [] {"german", "english"}, 
            "english", 
            () =>  LanguageChanged?.Invoke()
        );

        public static readonly StringListPreference DisplayTheme = new StringListPreference("Theme", 
            () => {
                Logger.Log("[Preferences]", "DisplayTheme: Loaded Themes:", ThemeHelper.Themes.Select(t => t.Name).ToFString());
                return ThemeHelper.Themes.Select(s => s.Name).ToArray();
            },
            ThemeHelper.Themes.FirstOrDefault(s => s.Name.ToLowerInvariant() == "default")?.Name ?? ThemeHelper.Themes.FirstOrDefault()?.Name, 
            () => ThemeChanged?.Invoke()
        );
        
        public static readonly StringPreference MoodleUrl = new StringPreference("Moodle: Server Url", "", MoodleHelper.Initialize, "https://...");

        // sidebar & titlebar
        public static readonly StringListPreference SchoolsList = new StringListPreference("Available Schools", RepresentationPlanManager.AvailableSchools.Keys.ToArray(), RepresentationPlanManager.AvailableSchools.Keys.ToArray()[0]);
        public static readonly StringPreference SchoolGrade = new StringPreference("Grade");
        public static readonly IntegerPreference MaxRecentFilesShown = new IntegerPreference("Max. Files Count in Recent Files", 10);
        
        public static readonly SidebarStatePreference SidebarStates = new SidebarStatePreference("Anpassen wann Sidebars angezeigt werden sollen", new Dictionary<Type, (bool, bool)> {
            {typeof(EditorScreen), (true, true)},
            {typeof(FileManagerScreen), (true, true)},
            
            {typeof(FeedbackScreen), (false, false)},
            {typeof(ImageEditorScreen), (false, false)},
            {typeof(SettingsScreen), (false, false)},
            {typeof(AboutScreen), (false, false)},
            {typeof(CameraScreen), (false, false)},
        });
        
        public static readonly SidebarItemsPreference LeftSidebarItems = new SidebarItemsPreference("Linke Sidebar anpassen (benötigt Neustart der App)", new List<SidebarItemModel> {
            new SidebarItemModel(typeof(CustomSearchView), "Search", Icon.Search),
            new SidebarItemModel(typeof(SidebarDocumentList), "Documents", Icon.DocumentsFolder),
            new SidebarItemModel(typeof(CustomRecentFilesView), "Recent", Icon.Recent),
        });
        
        public static readonly SidebarItemsPreference RightSidebarItems = new SidebarItemsPreference("Rechte Sidebar anpassen (benötigt Neustart der App)", new List<SidebarItemModel> {
            // disabled by default
            new SidebarItemModel(typeof(CustomCalculatorView), "Calculator", Icon.Calculator, false), 
            new SidebarItemModel(typeof(CustomRepresentationPlanView), "Representation Plan", Icon.Planner, false), 
            new SidebarItemModel(typeof(CustomNotesView), "Notes", Icon.Note, false), 
            
            // defaults collection
            new SidebarItemModel(typeof(CustomTasksView), "Tasks", Icon.TodoList), 
            new SidebarItemModel(typeof(CustomTimeTableView), "TimeTable", Icon.Timetable), 
            new SidebarItemModel(typeof(SidebarBrowserView), "Browser", Icon.Circle), 
            new SidebarItemModel(typeof(SidebarThesaurus), "Thesaurus", Icon.List),
            new SidebarItemModel(typeof(CustomFormulaView), "Formula Collection", Icon.Add),
            new SidebarItemModel(typeof(SidebarMoodleView), "Moodle", Icon.TaskPlanning),
            
            // editor only
            new SidebarItemModel(typeof(CustomSymbolsView), "Symbols", Icon.Plus, editorScreenOnly: true),
            new SidebarItemModel(typeof(SidebarFileBookmarks), "File Bookmarks", Icon.Bookmark, editorScreenOnly: true),
        });
        
        // fms
        public static readonly BooleanPreference ShowPathInTitle = new BooleanPreference("Show Path in File Manager Title", true, () => App.FileManagerScreen.NavigateToDirectory(App.FileManagerScreen.CurrentDirectory));

        // edt
        public static readonly BooleanPreference ColoredTitlebarEnabled = new BooleanPreference("Colored Titlebar");
        public static readonly EnumPreference<DocumentLineMode> BackgroundDefaultLineMode = new EnumPreference<DocumentLineMode>("Background Default Line Mode", DocumentLineMode.GridMedium);

        public static readonly DoublePreference ZoomMinScale = new DoublePreference("Zoom Min Scale", 1.0, () => {
            if (App.EditorScreen != null) {
                App.EditorScreen.ScrollMinZoom = ZoomMinScale;
            }
        }, 0.1, 2.0);
        public static readonly DoublePreference ZoomMaxScale = new DoublePreference("Zoom Max Scale", 4.0, () => {
            if (App.EditorScreen != null) {
                App.EditorScreen.ScrollMaxZoom = ZoomMaxScale;
            }
        }, 2.1, 10.0);
        
        // edt - drawing
        public static readonly BooleanPreference LongPressConversionEnabled = new BooleanPreference("Ink Conversion on Long Press", true);
        public static readonly IntegerPreference InkConversionTolerance = new IntegerPreference("Ink Conversion Tolerance", 5);
        public static readonly IntegerPreference InkConversionTime = new IntegerPreference("Ink Conversion Long Press Time", 200);
        public static readonly BooleanPreference InkAdjustLines = new BooleanPreference("Ink Conversion Adjust Lines", true);
        public static readonly BooleanPreference MarkerInkConversionEnabled = new BooleanPreference("Convert Marker Ink");
        public static readonly BooleanPreference ConvertToLinesOnButtonPress = new BooleanPreference("Enable Line Conversion On Button Press");
        public static readonly DoublePreference ConversionThreshold = new DoublePreference("Conversion Threshold (0 - 100)", 92);
        public static readonly DoublePreference MinPenSize = new DoublePreference("Min. Pen Size", 1.0);
        public static readonly DoublePreference MaxPenSize = new DoublePreference("Max. Pen Size", 20.0);

        // shortcuts
        public static readonly ShortcutPreference CreateFileShortcut = new ShortcutPreference("Create File", new List<string> {"Control", "N"});
        public static readonly ShortcutPreference CreateDirectoryShortcut = new ShortcutPreference("Create Directory", new List<string> {"Control", "Shift", "N"});
        public static readonly ShortcutPreference SearchShortcut = new ShortcutPreference("Search", new List<string> {"Control", "F"});
        public static readonly ShortcutPreference CopyShortcut = new ShortcutPreference("Copy", new List<string> {"Control", "C"});
        public static readonly ShortcutPreference CutShortcut = new ShortcutPreference("Cut", new List<string> {"Control", "X"});
        public static readonly ShortcutPreference PasteShortcut = new ShortcutPreference("Paste", new List<string> {"Control", "V"});
        public static readonly ShortcutPreference MoveToForegroundShortcut = new ShortcutPreference("Move to foreground", new List<string> {"Control", "Up"});
        public static readonly ShortcutPreference MoveToBackgroundShortcut = new ShortcutPreference("Move to background", new List<string> {"Control", "Down"});
        public static readonly ShortcutPreference ResetZoomShortcut = new ShortcutPreference("Reset Zoom", new List<string> {"Control", "F5"});
        public static readonly ShortcutPreference UndoShortcut = new ShortcutPreference("Undo", new List<string> {"Control", "Z"});
        public static readonly ShortcutPreference RedoShortcut = new ShortcutPreference("Redo", new List<string> {"Control", "Y"});
        public static readonly ShortcutPreference SelectAllShortcut = new ShortcutPreference("Select All", new List<string> {"Control", "A"});
        public static readonly ShortcutPreference DeleteShortcut = new ShortcutPreference("Delete", new List<string> {"Delete"});
        public static readonly ShortcutPreference RenameShortcut = new ShortcutPreference("Rename", new List<string> {"F2"});
        public static readonly ShortcutPreference FullscreenShortcut = new ShortcutPreference("Fullscreen", new List<string> {"F11"});
        public static readonly ShortcutPreference CancelShortcut = new ShortcutPreference("Cancel", new List<string> {"Escape"});
        public static readonly ShortcutPreference FeedbackShortcut = new ShortcutPreference("Fullscreen", new List<string> {"F1"});
        public static readonly ShortcutPreference RotateShortcut = new ShortcutPreference("Rotate Image", new List<string> {"R"});
        public static readonly ShortcutPreference InvertSelectionShortcut = new ShortcutPreference("Invert Selection", new List<string> {"Control", "I"});
        public static readonly ShortcutPreference ReloadShortcut = new ShortcutPreference("Reload", new List<string> {"F5"});
        
        // theming
        public static readonly StringPreference BackgroundCanvasLineColor = new StringPreference("Background Line Color", "#EFF0F1");
        public static readonly StringPreference BackgroundCanvasColor = new StringPreference("Background Color (hex)", "#FFFFFF");
        public static readonly IntegerPreference BackgroundCanvasOpacity = new IntegerPreference("Background Line Opacity (0 - 100)", 100);
        
        public static readonly IntegerPreference BackgroundLineModeStepsSmall = new IntegerPreference("BackgroundLineModeSteps Small", 16);
        public static readonly IntegerPreference BackgroundLineModeStepsMedium = new IntegerPreference("BackgroundLineModeSteps Medium", 25);
        public static readonly IntegerPreference BackgroundLineModeStepsLarge = new IntegerPreference("BackgroundLineModeSteps Large", 56);
        
        public static readonly BooleanPreference UseAnimatedIcons = new BooleanPreference("Use animated icons where possible");
        
        // labels
        public static readonly CustomLabelListPreference CustomLabels = new CustomLabelListPreference("Custom Labels");
        
        // user components
        public static readonly ComponentListPreference ExternalComponents = new ComponentListPreference("External Components");
        
        // advanced
        public static readonly BooleanPreference LocalSharingEnabled = new BooleanPreference("Enable Local Sharing", true, () => {
            if (LocalSharingEnabled) {
                LocalSharingHelper.SocketService.StartBluetoothServer();
            } else {
                LocalSharingHelper.SocketService.StopBluetoothServer();
            }
        });
        public static readonly IntegerPreference DoubleTapTime = new IntegerPreference("Double Tap Time (ms)", 500);
        public static readonly StringPreference ServerUrl = new StringPreference("Synchronization Server", "https://srv1.ainotes.xyz", () => Task.Run(SynchronizationService.Restart));

        // backup & restore
        public static readonly InvocationPreference CreateBackup = new InvocationPreference("Create Backup", () => {
            var popup = new MDContentPopup("Backup", new MDLabel(""), closeWhenBackgroundIsClicked: true, cancelable: true, okCallback: async () => {
                var (_, _, path) = await FilePicker.PickSaveFile("AINotes Backup.zip", "Save");

                ((MDButton) CreateBackup.GetView()).IsEnabled = false;
                ((MDButton) CreateBackup.GetView()).Text = "Process is being prepared...";

                try {
                    ZipFile.CreateFromDirectory(LocalFileHelper.ToAbsolutePath(""), path, CompressionLevel.NoCompression, true);
                } catch (Exception ex) {
                    Logger.Log("[Preferences]", "Exception in CreateBackup - Zip Creation:", ex.ToString(), logLevel: LogLevel.Error);
                    ((MDButton) CreateBackup.GetView()).Text = "Vorgang fehlgeschlagen. Bitte versuche es erneut oder kontaktiere den Support.";
                }

                ((MDButton) CreateBackup.GetView()).Text = "Vorgang beendet.";

                await Task.Run(() => {
                    Thread.Sleep(1000);
                    MainThread.BeginInvokeOnMainThread(() => {
                        try {
                            if (App.Page.Content != App.SettingsScreen) return;
                            ((MDButton) CreateBackup.GetView()).Text = "Create Backup";
                            ((MDButton) CreateBackup.GetView()).IsEnabled = true;
                        } catch (Exception ex) {
                            Logger.Log("[Preferences]", "Exception in CreateBackup - Button Restore:", ex.ToString());
                        }
                    });
                });
            });
            
            PopupNavigation.OpenPopup(popup);
        });
        public static readonly InvocationPreference RestoreBackup = new InvocationPreference("Restore Backup", () => {
            
        });


        // developer
        public static readonly BooleanPreference DeveloperModeEnabled = new BooleanPreference("Enable / Disable Developer Mode");
        public static readonly BooleanPreference DebugLabelsEnabled = new BooleanPreference("Enable / Disable Debug Labels (should be used with Border Debug Mode enabled)");
        public static readonly BooleanPreference FocusDebugModeEnabled = new BooleanPreference("Enable / Disable Focus Debugging");
        
        public static readonly BooleanPreference PartyModeEnabled = new BooleanPreference("PartyMode!");

        public static BooleanPreference LoggingEnabled => new BooleanPreference("Enable / Disable Logs", true);
        public static StringListPreference MinimumLogLevel => new StringListPreference("Minimal Logging Level", Enum.GetValues(typeof(LogLevel)).Cast<LogLevel>().Select(c => c.ToString()).ToArray(), LogLevel.Timing.ToString());
        public static readonly BooleanPreference BorderDebugModeEnabled = new BooleanPreference("Border Debug Mode");
        
        public static readonly InvocationPreference ClearPens = new InvocationPreference("Reset Saved Pens", () => SavedStatePreferenceHelper.Set("saved_pen_models", "{}"));

        public static readonly BooleanPreference QuickSavesEnabled = new BooleanPreference("Enable / Disable Auto Saves", true);
        public static readonly IntegerPreference QuickSavesInterval = new IntegerPreference("Auto Save Interval (in ms)", 1000);
        
        public static readonly IntegerPreference ServerUpdateRequestTimeout = new IntegerPreference("Update Request Interval", 20);
        
        public static readonly InvocationPreference RestartBluetoothServer = new InvocationPreference("Restart Bluetooth Server", null);
        public static readonly InvocationPreference CleanDatabase = new InvocationPreference("Datenbank aufräumen", () => {
            new MDContentPopup(
                "Möchtest du wirklich ungenutzte Einträge aus der Datenbank löschen?",
                new MDLabel("Diese Aktion kann die Performance der App verbessern, führt jedoch oft zu Problemen mit der Cloud-Synchronisation."),
                closeWhenBackgroundIsClicked: true,
                cancelable: true,
                okCallback: async () => {
                    ((MDButton) CleanDatabase.GetView()).IsEnabled = false;
                    ((MDButton) CleanDatabase.GetView()).Text = "Vorgang wird vorbereitet...";
                    var deletedEntries = await FileHelper.CleanDatabaseAsync(dE => {
                        ((MDButton) CleanDatabase.GetView()).Text = $"In Bearbeitung: {dE} Einträge gelöscht.";
                    });
                    ((MDButton) CleanDatabase.GetView()).Text = $"Vorgang beendet. {deletedEntries} Einträge gelöscht.";
                }
            ).Show();
        });
        public static readonly InvocationPreference ResetDatabase = new InvocationPreference("Reset Database", () => {
            var popup = new MDContentPopup("Delete all files and directories?", new MDLabel(""), closeWhenBackgroundIsClicked: true, cancelable: true, okCallback: async () => {
                ((MDButton) ResetDatabase.GetView()).IsEnabled = false;
                ((MDButton) ResetDatabase.GetView()).Text = "Process is being prepared...";

                await FileHelper.ClearDatabase();

                ((MDButton) ResetDatabase.GetView()).Text = "Finished successfully.";
                Thread.Sleep(1000);
                
                ((MDButton) ResetDatabase.GetView()).IsEnabled = true;
                ((MDButton) ResetDatabase.GetView()).Text = "Reset Database.";
            });
            
            PopupNavigation.OpenPopup(popup);
        });
        
        public static Dictionary<string, List<Preference>> GetSettings() {
            return new Dictionary<string, List<Preference>> {
                {
                    "General",
                    new List<Preference> {
                        DisplayLanguage,
                        DisplayTheme,
                        MoodleUrl,
                    }
                }, {
                    "Sidebar & Titlebar",
                    new List<Preference> {
                        SchoolsList,
                        SchoolGrade,
                        MaxRecentFilesShown,
                        SidebarStates,
                        LeftSidebarItems,
                        RightSidebarItems,
                    }
                }, {
                    "File Manager",
                    new List<Preference> {
                        ShowPathInTitle,
                    }
                }, {
                    "Editor",
                    new List<Preference> {
                        ColoredTitlebarEnabled,
                        BackgroundDefaultLineMode,
                        ZoomMinScale,
                        ZoomMaxScale,
                    }
                }, {
                    "Drawing",
                    new List<Preference> {
                        LongPressConversionEnabled,
                        InkConversionTolerance,
                        InkConversionTime,
                        InkAdjustLines,
                        MarkerInkConversionEnabled,
                        ConvertToLinesOnButtonPress,
                        ConversionThreshold,
                        MinPenSize,
                        MaxPenSize,
                    }
                }, {
                    "Shortcuts",
                    new List<Preference> {
                        CreateFileShortcut,
                        CreateDirectoryShortcut,
                        SearchShortcut,
                        CopyShortcut,
                        CutShortcut,
                        PasteShortcut,
                        MoveToForegroundShortcut,
                        MoveToBackgroundShortcut,
                        ResetZoomShortcut,
                        UndoShortcut,
                        RedoShortcut,
                        SelectAllShortcut,
                        DeleteShortcut,
                        RenameShortcut,
                        FullscreenShortcut,
                        CancelShortcut,
                        FeedbackShortcut,
                        RotateShortcut,
                    }
                }, {
                    "Labels",
                    new List<Preference> {
                        CustomLabels,
                    }
                }, {
                    "UserComponentSettings",
                    new List<Preference> {
                        ExternalComponents,
                    }
                }, {
                    "Advanced",
                    new List<Preference> {
                        DoubleTapTime,
                        LocalSharingEnabled,
                        ServerUrl,
                    }
                }, {
                    "Advanced Theming",
                    new List<Preference> {
                        BackgroundCanvasLineColor,
                        BackgroundCanvasColor,
                        BackgroundCanvasOpacity,
                        BackgroundLineModeStepsSmall,
                        BackgroundLineModeStepsMedium,
                        BackgroundLineModeStepsLarge,
                        UseAnimatedIcons
                    }
                }, {
                   "Backup & Restore",
                   new List<Preference> {
                       CreateBackup,
                       RestoreBackup,
                   } 
                }, {
                    "Developer Settings",
                    new List<Preference> {
                        DeveloperModeEnabled,
                        DebugLabelsEnabled,
                        FocusDebugModeEnabled,
                        PartyModeEnabled,
                        LoggingEnabled,
                        MinimumLogLevel,
                        BorderDebugModeEnabled,
                        ClearPens,
                        QuickSavesEnabled,
                        QuickSavesInterval,
                        ServerUpdateRequestTimeout,
                        RestartBluetoothServer,
                        CleanDatabase,
                        ResetDatabase,
                    }
                }
            };
        }

        public static void Initialize() {
            Logger.Log("[Preferences]", "-> Initialize");

            // Party Mode
            if (PartyModeEnabled) {
                ((ToggleSwitch) PartyModeEnabled.GetView()).IsOn = false;
            }
            
            PartyModeEnabled.Changed += () => {
                var partyThread = new Thread(async () => {
                    var progress = 0.0f;
                    const float speed = 0.015f;
                    const int sleep = 10;
            
                    var childOffsets = new Dictionary<object, float>();

                    while (true) {
                        if (!PartyModeEnabled) return;
                        await MainThread.InvokeOnMainThreadAsync(() => {
                            var allChildren = App.Page.ListChildren();
                            foreach (var child in allChildren) {
                                var t = child.GetType();
                                
                                // check if size properties exist
                                var widthProperty = t.GetProperty("ActualWidth");
                                var heightProperty = t.GetProperty("ActualHeight");
                                if (widthProperty == null || heightProperty == null) continue;
                                
                                // check size properties
                                var w = (double) widthProperty.GetValue(child);
                                var h = (double) heightProperty.GetValue(child);
                                if (w > 500 || h > 500 || w >= App.Page.ActualWidth / 2 || h >= App.Page.ActualHeight / 2 || double.IsNaN(h) || h == double.NaN || double.IsNaN(w) || w == double.NaN) continue;
                                
                                // check if background property exists
                                var backgroundProperty = t.GetProperty("Background");
                                if (backgroundProperty != null) {
                                    // store offset
                                    if (!childOffsets.ContainsKey(child)) {
                                        childOffsets.Add(child, (float) new System.Random().NextDouble());
                                    }

                                    // set background
                                    backgroundProperty.SetValue(child, ColorHelper.Rainbow(progress + childOffsets[child]).ToBrush(), null);
                                }
                                
                                // check if background property exists
                                var foregroundProperty = t.GetProperty("Foreground");
                                if (foregroundProperty != null) {
                                    // store offset
                                    if (!childOffsets.ContainsKey(child)) {
                                        childOffsets.Add(child, (float) new System.Random().NextDouble());
                                    }

                                    // set foreground
                                    foregroundProperty.SetValue(child, ColorHelper.Rainbow(progress + childOffsets[child] * 2).ToBrush(), null);
                                }
                                
                                // check if source property exists
                                var sourceProperty = t.GetProperty("Source");
                                if (sourceProperty != null) {
                                    var value = sourceProperty.GetValue(child);
                                    const string toastValue = "https://cdn.discordapp.com/emojis/651864261248679946.gif";
                                    
                                    switch (value) {
                                        case string sourceString: {
                                            if (sourceString != toastValue) {
                                                sourceProperty.SetValue(child, toastValue);
                                            }

                                            break;
                                        }
                                        case Uri sourceUri: {
                                            if (sourceUri != new Uri(toastValue)) {
                                                sourceProperty.SetValue(child, new Uri(toastValue));
                                            }

                                            break;
                                        }
                                    }
                                }
                            }
        
                            Titlebar.SetColor(ColorHelper.Rainbow(progress));
                        });
            
                        progress += speed;
                        Thread.Sleep(sleep);
                    }
                });
                if (PartyModeEnabled) {
                    partyThread.Start();
                }
            };
            
            Logger.Log("[Preferences]", "<- Initialize");
        }
    }
}