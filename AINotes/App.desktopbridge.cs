using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using AINotes.Controls.Popups;
using Helpers.Essentials;
using Helpers.Extensions;
using Windows.ApplicationModel.Activation;
using Helpers;
using MaterialComponents;

namespace AINotes {
    public partial class App {
        public static BackgroundTaskDeferral AppServiceDeferral;
        public static AppServiceConnection Connection;
        public static event EventHandler AppServiceDisconnected;
        public static event EventHandler<AppServiceTriggerDetails> AppServiceConnected;
        public static bool IsForeground = false;


        private  void LaunchService() {
            if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0)) {
                AppServiceConnected += OnAppServiceConnected;
                AppServiceDisconnected += OnAppServiceDisconnected;
                FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            } else {
                #if DEBUG
                throw new ApplicationException("Windows.ApplicationModel.FullTrustAppContract not present");
                #endif
            }
        }
        
        private void OnAppServiceConnected(object sender, AppServiceTriggerDetails e) {
            Connection.RequestReceived += OnRequestReceived;
        }

        private async void OnAppServiceDisconnected(object sender, EventArgs e) {
            if (IsForeground) {
                Logger.Log("[App]", "OnAppServiceDisconnected: Restarting Service");
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            } else {
                Logger.Log("[App]", "OnAppServiceDisconnected: Exiting");
                Current.Exit();
            }
        }

        public static async Task<AppServiceResponse> SendToAppService(ValueSet request) {
            Logger.Log("[App]", "SendToAppService", request.Keys.ToFString(), request.Keys.ToFString(), Connection == null);
            if (Connection != null) return await Connection.SendMessageAsync(request);
            Logger.Log("[App]", "SendToAppService: Connection is null.", logLevel: LogLevel.Error);
            Page.Notifications.Add(new MDNotification("Internal Error:\nRequest failed."));
            return null;
        }

        private void HandleAppServiceBackgroundActivation(BackgroundActivatedEventArgs args, AppServiceTriggerDetails details) {
            if (details.CallerPackageFamilyName != Package.Current.Id.FamilyName) return;
            AppServiceDeferral = args.TaskInstance.GetDeferral();
            args.TaskInstance.Canceled += OnTaskCanceled;
            Connection = details.AppServiceConnection;
            AppServiceConnected?.Invoke(this, args.TaskInstance.TriggerDetails as AppServiceTriggerDetails);
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args) {
            Logger.Log("[App]", "OnRequestReceived");
            foreach (var key in args.Request.Message.Keys) {
                switch (key) {
                    case "content":
                        args.Request.Message.TryGetValue("content", out var message);
                        if (IsForeground) {
                            await Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => {
                                var dialog = new MessageDialog(message?.ToString() ?? "empty message");
                                await dialog.ShowAsync();
                            });
                        } else {
                            ToastHelper.ShowToast(message?.ToString() ?? "empty message");
                        }

                        break;
                    case "start":
                        break;
                    case "exit":
                        Current.Exit();
                        break;
                    default:
                        new MDContentPopup("AppServiceConnection received unknown command", new StackPanel {
                            Children = {
                                new MDLabel {Text = "Unknown Command: " + key}
                            }
                        }).Show();
                        break;
                }
            }
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason) {
            Logger.Log("[App]", "OnTaskCanceled");
            AppServiceDeferral?.Complete();
            AppServiceDeferral = null;
            Connection = null;
            AppServiceDisconnected?.Invoke(this, null);
        }
    }
}