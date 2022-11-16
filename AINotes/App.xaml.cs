using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Data.Pdf;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AINotes.Controls.FileManagement;
using AINotes.Controls.Pages;
using AINotes.Controls.Popups;
using Helpers;
using Helpers.Essentials;
using Helpers.Extensions;
using AINotes.Screens;
using Sentry;
using AppInfo = Helpers.Essentials.AppInfo;
using AINotes.Components.Tools;
using AINotes.Helpers;
using AINotes.Models;
using AINotesCloud;
using MaterialComponents;

#if DEBUG
using System.Diagnostics;
#else
using Sentry.Protocol;
#endif

namespace AINotes {
    sealed partial class App {
        // static access
        public new static App Current => (App) Application.Current;
        public static CustomContentPage Page => (Window.Current?.Content as Frame)?.Content as CustomContentPage;

        // screens
        public static FileManagerScreen FileManagerScreen { get; private set; }
        public static EditorScreen EditorScreen { get; private set; }
        public static AboutScreen AboutScreen { get; private set; }
        public static FeedbackScreen FeedbackScreen { get; private set; }
        public static SettingsScreen SettingsScreen { get; private set; }
        public static CameraScreen CameraScreen { get; private set; }
        public static ImageEditorScreen ImageEditorScreen { get; private set; }

        // events
        public event EventHandler<WTouchEventArgs> Touch;

        public App() {
            Logger.Log("[App]", "-> *ctor");
            InitializeComponent();
            
            // init sentry
            var sentryOptions = new SentryOptions {
                Dsn = Configuration.LicenseKeys.Sentry,
                Release = AppInfo.VersionString,
#if DEBUG
                Environment = "debug",
#else
                Environment = "prod",
#endif

                IsGlobalModeEnabled = true,
                SendDefaultPii = true,
                AutoSessionTracking = true,
                MaxBreadcrumbs = 50,

                AttachStacktrace = true,
                StackTraceMode = StackTraceMode.Enhanced,

                TracesSampleRate = 1.0,
            };
            sentryOptions.DisableTaskUnobservedTaskExceptionCapture();
#if DEBUG
            sentryOptions.DisableAppDomainUnhandledExceptionCapture();
#endif
            SentrySdk.Init(sentryOptions);
            SentrySdk.StartSession();
            SentrySdk.ConfigureScope(scope => {
                scope.Contexts.OperatingSystem.Name = SystemInfo.GetSimpleSystemVersion(DeviceInfo.VersionString);
                scope.Contexts.OperatingSystem.Version = SystemInfo.GetSystemVersion(DeviceInfo.VersionString);
                scope.Contexts.OperatingSystem.Build = DeviceInfo.VersionString;

                scope.User.Id = SystemInfo.GetSystemId();
            });

            // events
            Suspending += OnSuspending;
            Resuming += OnResuming;
            
            EnteredBackground += OnEnteredBackground;
            LeavingBackground += OnLeavingBackground;
            
            UnhandledException += OnUnhandledException;

            Logger.Log("[App]", "<- *ctor");
        }

        private void Activate(string rootFrameArgs = null) {
            Logger.Log("[App]", "-> Activate");
            var rootFrame = (Frame) (Window.Current.Content ??= new Frame());

            if (rootFrame.Content == null) {
                Logger.Log("[App]", "Activate: Navigate");
                rootFrame.Navigate(typeof(CustomContentPage), rootFrameArgs);
                
                Logger.Log("[App]", "Activate: Subscribe to SystemNavigationManager");
                var mgr = SystemNavigationManagerPreview.GetForCurrentView();
                mgr.CloseRequested += OnCloseRequested;
                
                Logger.Log("[App]", "Activate: SetTouchEventHandler");
                TouchHelper.SetTouchEventHandler(rootFrame, args => Touch?.Invoke(this, args));

                // create an instance of all pages
                Logger.Log("[App]", "Activate: Create Pages");
                FileManagerScreen = new FileManagerScreen();
                EditorScreen = new EditorScreen();
                AboutScreen = new AboutScreen();
                FeedbackScreen = new FeedbackScreen();
                SettingsScreen = new SettingsScreen();
                CameraScreen = new CameraScreen();
                ImageEditorScreen = new ImageEditorScreen();
                
                // init
                Logger.Log("[App]", "Activate: Init");
                LocalFileHelper.CreateAppDirectories();
                
                Configuration.Initialize();
                Preferences.Initialize();

                SynchronizationService.Initialize();
                LocalSharingHelper.Initialize();

                // sys info
                Logger.Log("[App]", "Activate: System Info");
                SystemInfo.LogInfo();
                
                // shortcuts
                Logger.Log("[App]", "Activate: Shortcuts");
                RegisterShortcuts();

                Logger.Log("[App]", "Activate: Load");
                Page.Load(FileManagerScreen);
            }

            Logger.Log("[App]", "Activate: Activate Window");
            Window.Current.Activate();

            // reset titlebar
            if (EditorScreen == null || Page?.Content == null || Page?.Content != EditorScreen) {
                Logger.Log("[App]", "Activate: Reset Titlebar");
                var applicationTitleBar = ApplicationView.GetForCurrentView().TitleBar;
                var c = Configuration.Theme.Background;
                applicationTitleBar.BackgroundColor = applicationTitleBar.ButtonBackgroundColor = c.Color;
            }
            
            // launch service
            if (Connection == null) {
                Logger.Log("[App]", "Activate: Launch Service");
                LaunchService();
            }
            
            Logger.Log("[App]", "<- Activate");
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e) {
            Logger.Log("[App]", "-> OnLaunched", e.Kind, "|", e.User.Type);

            if (!e.PrelaunchActivated) {
                Activate(e.Arguments);
            }

            // print handler
            if (!string.IsNullOrEmpty(e.Arguments)) {
                Logger.Log("[App]", "OnLaunched: Arguments =", e.Arguments);

                static async void PrintInsert() {
                    var baseDir = ApplicationData.Current.LocalFolder.Path.Substring(0, ApplicationData.Current.LocalFolder.Path.Length - "LocalState".Length);
                
                    var acTemp = await StorageFolder.GetFolderFromPathAsync(baseDir+ "AC\\Temp\\");
                    var files = await acTemp.GetFilesAsync();
                    var f = files.OrderByDescending(sf => sf.DateCreated).First();
                    var pdfResult = await SendToAppService(new ValueSet {{"xps2pdf", (f.Path, LocalFileHelper.ToAbsolutePath("print.pdf")).Serialize()}});
                    Logger.Log("PdfResult:", pdfResult.Message.Keys.ToFString(), "=>", pdfResult.Message.Values.ToFString());
                    if ((string) pdfResult.Message["result"] == "ok") {
                        _ = f.DeleteAsync();
                        
                        var file = await StorageFile.GetFileFromPathAsync(LocalFileHelper.ToAbsolutePath("print.pdf"));
                        var fileStream = await file.OpenReadAsync();
                        var pdfDocument = await PdfDocument.LoadFromStreamAsync(fileStream);
                        if (Page.Content == EditorScreen) {
                            // insert into current file
                            ImageComponentTool.InsertPdfDocument(pdfDocument);
                        } else {
                            // ask user for file
                            var customListView = new CustomFileGridView();

                            var cancelButton = new MDButton {
                                ButtonStyle = MDButtonStyle.Secondary,
                                Text = "Cancel",
                                Command = () => {
                                    MDPopup.CloseCurrentPopup();
                                    fileStream.Dispose();
                                }
                            };
                            MDButton selectButton = null;
                            selectButton = new MDButton {
                                ButtonStyle = MDButtonStyle.Primary,
                                Text = "Select",
                                Command = () => {
                                    Page.Load(EditorScreen);
                                    var fileId = (customListView.SelectedModels.FirstOrDefault(m => m is FileModel) as FileModel)?.FileId;
                                    if (!(fileId is { } fId)) {
                                        if (selectButton == null) return;
                                        selectButton.ButtonStyle = MDButtonStyle.Error;
                                        return;
                                    }

                                    if (selectButton != null) {
                                        selectButton.ButtonStyle = MDButtonStyle.Primary;
                                    }
                                    
                                    EditorScreen.LoadFile(fId);
                                    if (!EditorScreen.IsPageLoaded) {
                                        EditorScreen.PageLoaded += () => ImageComponentTool.InsertPdfDocument(pdfDocument);
                                    } else {
                                        ImageComponentTool.InsertPdfDocument(pdfDocument);
                                    }
                                    
                                    MDPopup.CloseCurrentPopup();
                        
                                    fileStream.Dispose();
                                },
                            };

                            new MDContentPopup("Select File to insert Print", new Frame {
                                Background = Configuration.Theme.Background,
                                Width = Page.ActualWidth - 100,
                                Height = Page.ActualHeight - 200,
                                Margin = new Thickness(15),
                                Content = new Grid {
                                    ColumnSpacing = 10,
                                    ColumnDefinitions = {
                                        new ColumnDefinition {Width = new GridLength(11, GridUnitType.Star)},
                                        new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)},
                                    },
                                    Children = {
                                        {customListView, 0, 0}, {
                                            new StackPanel {
                                                Children = {
                                                    selectButton,
                                                    new Frame {Height = 5},
                                                    cancelButton,
                                                }
                                            },
                                            0, 1
                                        }
                                    }
                                }
                            }).Show();
                        }
                    } else {
                        new MDContentPopup("Error", new MDLabel("Der Druck konnte nicht eingefügt werden")).Show();
                    }
                }
                
                if (Connection == null) {
                    Logger.Log("[App]", "OnLaunched: Print - Waiting for Service", e.Arguments);
                    static void AppServiceWaiter(object s, object a) {
                        try {
                            PrintInsert();
                        } catch (Exception ex) {
                            Logger.Log("[App]", "OnLaunched: Print - Waiting for Service - PrintInsert failed:", ex, logLevel: LogLevel.Error);
                            Page.Notifications.Add(new MDNotification("Insert failed. :("));
                            SentryHelper.CaptureCaughtException(ex);
                        }

                        AppServiceConnected -= AppServiceWaiter;
                    }

                    AppServiceConnected += AppServiceWaiter;
                } else {
                    Logger.Log("[App]", "OnLaunched: Print - Inserting", e.Arguments);
                    try {
                        PrintInsert();
                    } catch (Exception ex) {
                        Logger.Log("[App]", "OnLaunched: Print - Inserting - PrintInsert failed:", ex, logLevel: LogLevel.Error);
                        Page.Notifications.Add(new MDNotification("Insert failed. :("));
                        SentryHelper.CaptureCaughtException(ex);
                    }
                }
            }
            
            AnalyticsHelper.SendEvent("Launched");
            Logger.Log("[App]", "<- OnLaunched");
        }

        private void OnCloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e) {
            Logger.Log("[App]", "OnCloseRequested");
            var deferral = e.GetDeferral();
            IsForeground = false;
            SentrySdk.EndSession();
            AnalyticsHelper.SendEvent("Closed");
            deferral.Complete();
        }

        private async void OnSuspending(object sender, SuspendingEventArgs e) {
            Logger.Log("[App]", "OnSuspending");
            var deferral = e.SuspendingOperation.GetDeferral();
            SentrySdk.PauseSession();
            await SentrySdk.FlushAsync(TimeSpan.FromSeconds(2));
            deferral.Complete();
            AnalyticsHelper.SendEvent("Suspended");
        }

        private void OnResuming(object sender, object o) {
            Logger.Log("[App]", "OnResuming");
            AnalyticsHelper.SendEvent("Resumed");
            SentrySdk.ResumeSession();
        }
        
        private void OnEnteredBackground(object sender, EnteredBackgroundEventArgs e) {
            Logger.Log("[App]", "OnEnteredBackground");
            IsForeground = false;
            SentrySdk.PauseSession();
        }

        private void OnLeavingBackground(object sender, LeavingBackgroundEventArgs e) {
            Logger.Log("[App]", "OnLeavingBackground");
            IsForeground = true;
            SentrySdk.ResumeSession();
        }
        
        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        private void OnUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e) {
            Logger.Log("[App]", "UnhandledException:", e.Exception?.ToString(), logLevel: LogLevel.Error);
            AnalyticsHelper.SendEvent("Closed", $"Exception: {e.Message}");

            #if DEBUG
            if (Debugger.IsAttached) Debugger.Break();
            #else
            var exception = e.Exception;
            var message = e.Message;
            if (exception == null) return;
            exception.Data[Mechanism.HandledKey] = false;
            exception.Data[Mechanism.MechanismKey] = "Application.UnhandledException";

            SentrySdk.ConfigureScope(scope => {
                scope.SetTag("Exception-Sender", sender.ToString());
            });
            SentrySdk.AddBreadcrumb(message, level: BreadcrumbLevel.Critical);
            SentrySdk.CaptureException(exception);
            SentrySdk.FlushAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
            SentrySdk.EndSession(SessionEndStatus.Crashed);
            SentrySdk.Close();
            #endif
        }
    }
}