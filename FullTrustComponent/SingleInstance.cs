using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Newtonsoft.Json;
using Application = System.Windows.Application;
using StartupEventArgs = Microsoft.VisualBasic.ApplicationServices.StartupEventArgs;


namespace FullTrustComponent {
    public class SingleInstanceManager : WindowsFormsApplicationBase {
        private SingleInstanceApplication _application;

        public SingleInstanceManager() {
            IsSingleInstance = true;
        }

        protected override bool OnStartup(StartupEventArgs eventArgs) {
            _application = new SingleInstanceApplication();
            _application.Run();
            return false;
        }
    }

    public class SingleInstanceApplication : Application {
        private AppServiceConnection _connection;
        private readonly NotifyIcon _notifyIcon = new NotifyIcon();

        #if DEBUG
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool AllocConsole();
        #else
        // ReSharper disable once UnusedMethodReturnValue.Local
        private static bool AllocConsole() => true;
        #endif
        
        #if DEBUG
        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, System.Text.StringBuilder packageFullName);
        private bool IsPackaged() {
            var length = 0;
            var sb = new System.Text.StringBuilder(0);
            GetCurrentPackageFullName(ref length, sb);
            sb = new System.Text.StringBuilder(length);
            var result = GetCurrentPackageFullName(ref length, sb);
            return result != 15700L;
        }
        #else
        private bool IsPackaged() => true;
        #endif

        protected override void OnStartup(System.Windows.StartupEventArgs e) {
            base.OnStartup(e);
            AllocConsole();
            
            // NotifyIcon
            _notifyIcon.Icon = FullTrustComponent.Properties.Resources.NotifyIcon;
            _notifyIcon.Visible = true;

            var openItem = new MenuItem {Text = @"Open", DefaultItem = true};
            var toggleDiscordItem = new MenuItem {Text = @"Toggle Discord RPC", Checked = true};
            var exitItem = new MenuItem {Text = @"Exit"};
            _notifyIcon.ContextMenu = new ContextMenu {
                Name = "AINotes",
                MenuItems = {
                    openItem,
                    toggleDiscordItem,
                    exitItem,
                }
            };

            _notifyIcon.DoubleClick += OpenCallback;
            openItem.Click += OpenCallback;
            toggleDiscordItem.Click += ToggleDiscordCallback;
            exitItem.Click += ExitCallback;
            
            // Init
            if (IsPackaged()) {
                InitializeAppServiceConnection();
            }

            if (FullTrustComponent.Properties.Settings.Default.DiscordRpcEnabled) {
                DiscordHelper.Initialize();
            }
        }

        private async void OpenCallback(object s, object a) {
            IEnumerable<AppListEntry> appListEntries = await Package.Current.GetAppListEntriesAsync();
            await appListEntries.First().LaunchAsync();
        }

        private void ToggleDiscordCallback(object s, object a) {
            ((MenuItem) s).Checked = !((MenuItem) s).Checked;
            
            if (((MenuItem) s).Checked) {
                if (!DiscordHelper.Connected) {
                    DiscordHelper.Initialize();
                }
            } else {
                if (DiscordHelper.Connected) {
                    DiscordHelper.Deinitialize();
                }
            }

            FullTrustComponent.Properties.Settings.Default.DiscordRpcEnabled = ((MenuItem) s).Checked;
            FullTrustComponent.Properties.Settings.Default.Save();
        }

        private async void ExitCallback(object s, object a) {
            await SendToUwp(new ValueSet {{"exit", ""}});
            Close();
        }
        
        private async void InitializeAppServiceConnection() {
            _connection = new AppServiceConnection {
                AppServiceName = "AINotesFullTrustComponent",
                PackageFamilyName = Package.Current.Id.FamilyName
            };
            
            _connection.RequestReceived += OnRequestReceived;
            _connection.ServiceClosed += OnServiceClosed;

            var status = await _connection.OpenAsync();
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (status == AppServiceConnectionStatus.Success) {
                Console.WriteLine(@"InitializeAppServiceConnection - Connected");
            } else {
                Console.WriteLine(@"InitializeAppServiceConnection - Something went wrong");
            }
        }
        
        private void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args) {
            foreach (var key in args.Request.Message.Keys) {
                Console.WriteLine(@"OnRequestReceived - {0}", key);
                switch (key) {
                    case "xps2pdf": {
                        var (inPath, outPath) = JsonConvert.DeserializeObject<(string, string)>((string) args.Request.Message[key]);
                        var success = FileFormatHelper.XpsToPdf(inPath, outPath);
                        args.Request.SendResponseAsync(new ValueSet {{"result", success ? "ok" : "error"}});
                        break;
                    }
                    case "json2pdf": {
                        var (noteJson, backgroundImage, backgroundImagePosString, outPath) = JsonConvert.DeserializeObject<(string, string, string, string)>((string) args.Request.Message[key]);
                        var success = FileFormatHelper.JsonToPdf(noteJson, backgroundImage, backgroundImagePosString, outPath);
                        args.Request.SendResponseAsync(new ValueSet {{"result", success ? "ok" : "error"}});
                        break;
                    }
                    case "setDiscordPresence": {
                        var (details, detailsState) = JsonConvert.DeserializeObject<(string, string)>((string) args.Request.Message[key]);
                        DiscordHelper.SetPresence(details, detailsState);
                        break;
                    }
                    default:
                        Console.WriteLine(@"OnRequestReceived - Unknown Command: {0}", key);
                        break;
                }
            }
        }
        
        private void OnServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args) {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(Close));
        }

        private bool _closeCalled;
        private void Close() {
            if (_closeCalled) return;
            _closeCalled = true;
            _notifyIcon.Dispose();
            DiscordHelper.Dispose();
            Current.Shutdown();
        }

        protected override void OnExit(ExitEventArgs e) {
            Close();
            base.OnExit(e);
        }

        private async Task SendToUwp(ValueSet request) {
            await _connection.SendMessageAsync(request);
        }

        [STAThread]
        public static void Main(string[] args) {
            new SingleInstanceManager().Run(args);
        }
    }
}