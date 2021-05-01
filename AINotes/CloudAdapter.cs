using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Windows.ApplicationModel;
using Windows.UI.Input.Inking;
using AINotes.Components;
using AINotes.Components.Implementations;
using AINotes.Helpers;
using AINotes.Helpers.Merging;
using AINotes.Models;
using AINotesCloud;
using AINotesCloud.Models;
using Helpers;
using Helpers.Essentials;
using Helpers.Extensions;

namespace AINotes {
    public static class CloudAdapter {
        public static readonly Dictionary<string, RemoteUserModel> UserCache = new Dictionary<string, RemoteUserModel>();
        
        static CloudAdapter() {
            SynchronizationService.Init(Preferences.ServerUrl);
            SynchronizationService.Start();

            SynchronizationService.Started += OnSynchronizationServiceStarted;

            CloudApi.AccountChanged += OnAccountChanged;

            App.Current.Resuming += OnAppResuming;
            App.Current.Suspending += OnAppSuspending;

            // restart client when url is changed
            Preferences.ServerUrl.Changed += OnServerUrlPreferenceChanged;

            // subscribe to editor events
            App.EditorScreen.ComponentAdded += OnComponentAdded;
            App.EditorScreen.ComponentChanged += OnComponentChanged;
            App.EditorScreen.ComponentDeleted += OnComponentDeleted;

            App.EditorScreen.StrokeAdded += OnStrokeAdded;
            App.EditorScreen.StrokeChanged += OnStrokeChanged;
            App.EditorScreen.StrokeRemoved += OnStrokeRemoved;
        }

        public static RemoteUserModel GetCachedRemoteUser(string userId, bool addToCacheAsyncIfNotFound=true) {
            if (userId == null) return null;
            if (IsLoggedIn && CurrentRemoteUserModel != null && userId == CurrentRemoteUserModel.RemoteId) {
                return CurrentRemoteUserModel;
            }
            
            if (UserCache.ContainsKey(userId)) {
                return UserCache[userId];
            }

            if (addToCacheAsyncIfNotFound) {
                Task.Run(async () => {
                    var remoteUser = await SynchronizationService.CloudApi.GetUser(userId);
                    UserCache.Add(userId, remoteUser);
                });

            }

            return null;
        }

        private static void OnSynchronizationServiceStarted() {
            SubscribeToRemote();
        }

        private static void OnAccountChanged() {
            AccountChanged?.Invoke();
        }

        private static void OnAppResuming(object s, object e) {
            SubscribeToRemote();
        }

        private static void OnAppSuspending(object s, SuspendingEventArgs e) {
            var deferral = e.SuspendingOperation.GetDeferral();
            UnsubscribeFromRemote();
            deferral.Complete();
        }

        private static void OnServerUrlPreferenceChanged() {
            UnsubscribeFromRemote();
            SynchronizationService.Init(Preferences.ServerUrl);
            SynchronizationService.Start();
        }


        #region File

        private static void OnComponentAdded(Component component) { }
        
        private static void OnComponentChanged(Component component) { }
        
        private static async void OnComponentDeleted(Component component) {
            if (component.ComponentId == -1) return;
            if (component is TextComponent textComponent) {
                if (textComponent.ShouldDeleteSelf()) return;
            }
            var componentModel = await FileHelper.GetComponentAsync(component.ComponentId);

            if (componentModel == null) {
                Logger.Log($"[{nameof(CloudAdapter)}]", $"{nameof(OnComponentDeleted)} - componentModel is null - aborting");
                return;
            }
            
            componentModel.Deleted = true;

            if (componentModel.RemoteId != null) {
                var remoteComponentModel = await SynchronizationService.CloudApi.GetRemoteComponent(componentModel.RemoteId);
                await ComponentMerger.MergeComponentDatabase(componentModel, remoteComponentModel);
            }
        }

        private static void OnStrokeAdded(InkStroke strokeTp) { }
        private static void OnStrokeChanged(InkStroke strokeTp) { }
        private static void OnStrokeRemoved(int strokeId) { }

        public static async Task<(bool, string)> UploadFile(FileModel fileModel) {
            var remoteId = await SynchronizationService.CloudApi.PostFile(fileModel.ToRemoteFileModel());

            // set remote id & last synced
            if (remoteId != null && (string.IsNullOrEmpty(fileModel.RemoteAccountId) || fileModel.RemoteAccountId == CurrentRemoteUserModel.RemoteId)) {
                await FileHelper.SetRemoteId(fileModel, remoteId);
                await FileHelper.SetLastSynced(fileModel);
                await FileHelper.SetRemoteAccount(fileModel, CurrentRemoteUserModel.RemoteId);
            }

            return (remoteId != null, remoteId);
        }

        public static async Task<bool> UpdateFile(FileModel fileModel) {
            if (CurrentRemoteUserModel?.RemoteId == null) return false;
            if (string.IsNullOrEmpty(fileModel.RemoteAccountId) || fileModel.RemoteAccountId == CurrentRemoteUserModel.RemoteId) {
                var success = await SynchronizationService.CloudApi.PutFile(fileModel.ToRemoteFileModel());
                await FileHelper.SetLastSynced(fileModel);
                await FileHelper.SetRemoteAccount(fileModel, CurrentRemoteUserModel.RemoteId);
                return success;
            }
            return false;
        }
        
        public static async Task<(bool, ComponentModel)> UploadComponent(ComponentModel componentModel) {
            if (componentModel.RemoteId == null) {
                var (s, remoteId) = await SynchronizationService.CloudApi.PostComponent(await componentModel.ToRemoteComponentModelAsync(true));
                
                if (!s) return (false, null);

                componentModel.RemoteId = remoteId;
                componentModel.LastSynced = Time.CurrentTimeMillis();
                return (true, componentModel);
            }

            var success = await SynchronizationService.CloudApi.PutComponent(await componentModel.ToRemoteComponentModelAsync());
            if (success) componentModel.LastSynced = Time.CurrentTimeMillis();
            return (success, componentModel);
        }

        public static async Task<RemoteFileModel> GetFile(string remoteFileId) {
            return await SynchronizationService.CloudApi.GetFile(remoteFileId);
        }

        #endregion

        #region General

        private static void AddSavedUser() {
            if (CurrentRemoteUserModel == null) return;

            var savedUsersSerialized = SavedStatePreferenceHelper.Get("savedUsers", "");
            var savedUsers = savedUsersSerialized == "" ? new Dictionary<string, RemoteUserModel>() : savedUsersSerialized.Deserialize<Dictionary<string, RemoteUserModel>>();

            if (savedUsers.Values.All(model => model.Email != CurrentRemoteUserModel.Email)) {
                savedUsers.Add(SynchronizationService.CloudApi.Token, CurrentRemoteUserModel);
            }

            SavedStatePreferenceHelper.Set("savedUsers", savedUsers.Serialize());
        }

        public static async Task<bool> Login(string email, string password) {
            var result = await SynchronizationService.Login(email, password);
            AddSavedUser();

            return result.success;
        }

        public static async Task<(bool, string)> TokenLogin(string token) {
            SavedStatePreferenceHelper.Set("cloudToken", token);
            return await SynchronizationService.CloudApi.TokenLogin();
        }

        public static void Restart() => SynchronizationService.Restart();

        public static void Logout() => SynchronizationService.PlaceLogout();

        public static void SubscribeToRemote() {
            Logger.Log("[CloudAdapter]", "Subscribing to remote");
            InitialCheck();
            SynchronizationService.CheckRemoteTimer.Interval = SynchronizationService.UpdateRequestTimeout;
            SynchronizationService.CheckRemoteTimer.Elapsed += CheckRemote;
            SynchronizationService.CheckRemoteTimer.Start();
        }

        public static void UnsubscribeFromRemote() {
            Logger.Log("[CloudAdapter]", "Unsubscribing from remote");
            SynchronizationService.CheckRemoteTimer.Elapsed -= CheckRemote;
        }

        public static readonly List<int> ExcludedFiles = new List<int>();

        private static async void InitialCheck() {
            if (SynchronizationService.BusySemaphore.CurrentCount == 0) return;
            if (!SynchronizationService.IsRunning) return;
            if (!SynchronizationService.CloudApi.IsLoggedIn) return;
            if (App.EditorScreen.Saving) return;
            try {
                await SynchronizationService.BusySemaphore.WaitAsync();

                Logger.Log("[CloudAdapter]", "-> InitialCheck", logLevel: LogLevel.Debug);

                // get files
                // TODO: Get only Updated & Deleted Files
                // var lastCheckedTimestamp = SavedStatePreferenceHelper.Get("sub2remoteLastChecked", 0.ToString());
                // var timeDifference = Time.CurrentTimeMillis() - long.Parse(lastCheckedTimestamp);
                // var remoteFilePermissions = await CloudApi.GetRemoteFilePermissionsChangedAfterTimestamp(lastCheckedTimestamp);
                var remoteFilePermissions = await SynchronizationService.CloudApi.GetFilePermissions();

                if (remoteFilePermissions == null) {
                    Thread.Sleep(1000);
                    SynchronizationService.BusySemaphore.Release();
                    InitialCheck();
                    return;
                }

                Logger.Log("[CloudAdapter]", "InitialCheck: RemoteFiles:", remoteFilePermissions.Count, logLevel: LogLevel.Debug);

                var localFiles = await FileHelper.ListFilesAsync(includeDeleted: true);
                Logger.Log("[CloudAdapter]", "InitialCheck: LocalFiles:", localFiles.Count, logLevel: LogLevel.Debug);

                // match local -> remote
                var fileModelTuples = MatchFiles(localFiles, remoteFilePermissions);

                // merge
                foreach (var (localFileModel, remoteFilePermission) in fileModelTuples) {
                    if (localFileModel == null) continue;
                    if (ExcludedFiles.Contains(localFileModel.Id)) continue;
                    ExcludedFiles.Add(localFileModel.Id);
                    var mergeResult = await FileMerger.Merge(localFileModel, remoteFilePermission);
                    
                    // Logger.Log("LocalFileModel: ", localFileModel, "  RemoteFilePermission:", remoteFilePermission);
                    Logger.Log("[CloudAdapter]", "InitialCheck: MergeResult:", mergeResult);
                }

                // set last check
                SavedStatePreferenceHelper.Set("sub2remoteLastChecked", Time.CurrentTimeMillis().ToString());
                Logger.Log("[CloudAdapter]", "<- InitialCheck", logLevel: LogLevel.Debug);
            } catch (Exception ex) when (ex.GetType() != typeof(NullReferenceException)) {
                Logger.Log("[CloudAdapter]", "InitialCheck: Error while checking for remote updates:", ex.ToString(), logLevel: LogLevel.Error);
            } finally {
                SynchronizationService.BusySemaphore.Release();
            }
        }

        private static async void CheckRemote(object _, ElapsedEventArgs __) {
            if (SynchronizationService.BusySemaphore.CurrentCount == 0) return;
            if (!SynchronizationService.IsRunning) return;
            if (!SynchronizationService.CloudApi.IsLoggedIn) return;
            if (App.EditorScreen.Saving) return;
            try {
                await SynchronizationService.BusySemaphore.WaitAsync();

                // get last check
                Logger.Log("[CloudAdapter]", "-> CheckRemote", logLevel: LogLevel.Debug);

                // get files
                // TODO: Get only Updated & Deleted Files
                // var lastCheckedTimestamp = SavedStatePreferenceHelper.Get("sub2remoteLastChecked", 0.ToString());
                // var timeDifference = Time.CurrentTimeMillis() - long.Parse(lastCheckedTimestamp);
                // var remoteFilePermissions = await CloudApi.GetRemoteFilePermissionsChangedAfterTimestamp(lastCheckedTimestamp);
                var remoteFilePermissions = await SynchronizationService.CloudApi.GetFilePermissions();
                if (remoteFilePermissions == null) return;
                Logger.Log("[CloudAdapter]", "CheckRemote: RemoteFiles:", remoteFilePermissions.Count, logLevel: LogLevel.Debug);

                var localFiles = await FileHelper.ListFilesAsync(includeDeleted: true);
                Logger.Log("[CloudAdapter]", "CheckRemote: LocalFiles:", localFiles.Count, logLevel: LogLevel.Debug);

                // match local -> remote
                var fileModelTuples = MatchFiles(localFiles, remoteFilePermissions);

                // merge
                foreach (var (localFileModel, remoteFilePermission) in fileModelTuples) {
                    // if (localFileModel != null) {
                    //     if (localFileModel.FileId != App.EditorScreen.FileId) {
                    //         if (remoteFilePermission != null) {
                    //             continue;
                    //         }
                    //     }
                    // }
                    var mergeResult = await FileMerger.Merge(localFileModel, remoteFilePermission); 
                    // Logger.Log("LocalFileModel: ", localFileModel, "  RemoteFilePermission:", remoteFilePermission);
                    Logger.Log("[CloudAdapter]", "InitialCheck: MergeResult:", mergeResult);
                }

                // var currentLocalFileModel = App.EditorScreen.LoadedFileModel;
                // if (currentLocalFileModel == null) return;
                //
                // // merging only current file
                // var index = fileModelTuples.ToList().FindIndex(tuple => tuple.Item1.Id == currentLocalFileModel.Id);
                // var mergeResult = await FileMerger.Merge(currentLocalFileModel, fileModelTuples[index].Item2);

                // set last check
                SavedStatePreferenceHelper.Set("sub2remoteLastChecked", Time.CurrentTimeMillis().ToString());
                App.FileManagerScreen.LoadFiles();
                Logger.Log("[CloudAdapter]", "<- CheckRemote", logLevel: LogLevel.Debug);
            } catch (Exception ex) when (ex.GetType() != typeof(NullReferenceException)) {
                Logger.Log("[CloudAdapter]", "CheckRemote: Error while checking for remote updates:", ex.ToString(), logLevel: LogLevel.Error);
            } finally {
                SynchronizationService.BusySemaphore.Release();
            }
        }

        private static (FileModel, RemoteFilePermission)[] MatchFiles(IEnumerable<FileModel> localFiles, List<RemoteFilePermission> remoteFilePermissions) {
            var fileModelTuples = new List<(FileModel, RemoteFilePermission)>();
            foreach (var localFileModel in localFiles.ToList()) {
                var remoteFilePermission = remoteFilePermissions.FirstOrDefault(rfp => rfp?.FileId == localFileModel.RemoteId);
                if (remoteFilePermission != null) {
                    remoteFilePermissions.Remove(remoteFilePermission);
                }

                fileModelTuples.Add((localFileModel, remoteFilePermission));
            }

            fileModelTuples.AddRange(remoteFilePermissions.Select(remoteFileModel => ((FileModel, RemoteFilePermission)) (null, remoteFileModel)));

            foreach (var tuple in fileModelTuples) {
                Logger.Log("Local FileModel: ", tuple.Item1?.FileId, "|", tuple.Item1?.RemoteId, "Remote FilePermission", tuple.Item2?.FileId);
            }

            return fileModelTuples.ToArray();
        }

        #endregion

        #region Account

        public static bool IsLoggedIn => SynchronizationService.CloudApi.IsLoggedIn;
        public static RemoteUserModel CurrentRemoteUserModel => CloudApi.CurrentRemoteUserModel;

        public static event Action AccountChanged;

        public static async Task<(bool success, string message)> Register(string displayname, string email, string password) {
            var success = await SynchronizationService.CloudApi.RegisterAndLogin(displayname, email, password);
            AddSavedUser();
            return success;
        }

        public static async Task<bool> ChangePassword(string oldPassword, string newPassword) {
            return await SynchronizationService.ChangePassword(oldPassword, newPassword);
        }

        public static async Task<bool> SetUserInfo(string displayName = "") {
            var userInfo = await GetUserInfo();
            if (displayName != "") userInfo.DisplayName = displayName;
            if (displayName == "") return false;
            return await SynchronizationService.CloudApi.PutUser(userInfo);
        }

        public static async Task<RemoteUserModel> GetUserInfo() => await SynchronizationService.CloudApi.GetUser();

        public static async Task<bool> UploadProfilePicture(Stream imageStream) {
            return await SynchronizationService.CloudApi.UploadProfilePicture(imageStream);
        }

        #endregion
    }
}