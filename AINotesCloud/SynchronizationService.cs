using System;
using System.Threading;
using System.Threading.Tasks;
using Helpers;
using Timer = System.Timers.Timer;

namespace AINotesCloud {
    public static class SynchronizationService {
        // cloud api
        public static CloudApi CloudApi;

        // preferences
        public const int UpdateRequestTimeout = 5000;

        // remote check timer (interval gets updated when subscribing to remote)
        public static readonly Timer CheckRemoteTimer = new Timer {
            Enabled = true,
            AutoReset = true,
            Interval = UpdateRequestTimeout,
        };

        // state
        public static bool IsRunning { get; private set; }

        public static void Init(string serverUrl) {
            CloudApi = new CloudApi(serverUrl);
        }

        static SynchronizationService() {
            // subscribe to app events
            CloudApi.AccountChanged += OnAccountChanged;
        }

        public static readonly SemaphoreSlim BusySemaphore = new SemaphoreSlim(1);

        private static async void OnAccountChanged() {
            if (CloudApi.IsLoggedIn) {
                if (!IsRunning) await Start();
            } else {
                // if (IsRunning) Stop();
            }
        }

        public static async void Initialize() {
            Logger.Log("[SynchronizationService]", "-> Initialize");
            await Start();
            Logger.Log("[SynchronizationService]", "<- Initialize");
        }

        public static event Action Started;

        public static async Task<(bool, string)> Start() {
            if (IsRunning) return (true, "Already running.");
            IsRunning = true;
            Logger.Log("[SynchronizationService]", "Starting", logLevel: LogLevel.Debug);

            bool success;
            string message;
            if (CloudApi != null) {
                try {
                    (success, message) = await CloudApi.TokenLogin();
                } catch (Exception ex) {
                    (success, message) = (false, ex.ToString());
                }
            } else {
                (success, message) = (false, "Not implemented.");
            }

            if (!success) {
                IsRunning = false;
                Logger.Log("[SynchronizationService]", "Not Started: Token login failed -", message, logLevel: LogLevel.Warning);
                return (false, $"Login failed ({message})");
            }

            Logger.Log("[SynchronizationService]", "Started", logLevel: LogLevel.Debug);
            Started?.Invoke();
            return (true, message);
        }

        public static void Stop() {
            Logger.Log("[SynchronizationService]", "Stopped");
            // UnsubscribeFromRemote();
            IsRunning = false;
        }

        public static async Task<(bool, string)> Restart() {
            Stop();
            return await Start();
        }

        public static async Task<(bool success, string message)> Login(string email, string password) => await CloudApi.Login(email, password);

        public static async Task<bool> ChangePassword(string oldPassword, string newPassword) => await CloudApi.ChangePassword(oldPassword, newPassword);


        public static async void PlaceLogout() {
            if (BusySemaphore.CurrentCount > 0) {
                try {
                    await BusySemaphore.WaitAsync();
                    CloudApi.Logout();
                } finally {
                    BusySemaphore.Release();
                }
            } else {
                CloudApi.Logout();
            }
        }
    }
}