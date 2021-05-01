using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using AINotes.Controls.Popups;
using AINotes.Models;
using AINotesCloud;
using AINotesCloud.Models;
using Helpers;
using Helpers.Essentials;
using MaterialComponents;

namespace AINotes.Helpers.Merging {
    public static class FileMerger {
        public static readonly Dictionary<RemoteFilePermission, RemoteFileModel> Invitations = new Dictionary<RemoteFilePermission, RemoteFileModel>();
        public static event Action ApprovalRequested;

        public static void InvokeApprovalRequested() => ApprovalRequested?.Invoke();

        public static async Task<bool> Merge(FileModel localFileModel, RemoteFilePermission remoteFilePermission) {
            Logger.Log("[FileMerger]", $"Merge -> Local: {localFileModel?.Name} ({localFileModel?.LastChangedDate}|{localFileModel?.LastSynced}) ({localFileModel?.FileId}|{localFileModel?.RemoteId})");

            if (localFileModel?.RemoteAccountId != null && localFileModel.RemoteAccountId != CloudAdapter.CurrentRemoteUserModel.RemoteId) {
                Logger.Log("[FileMerger]", "Wrong account. Aborting file merging.");
                return false;
            }

            // local file does not yet exist on server
            if (remoteFilePermission == null) {
                if (localFileModel?.RemoteId != null) {
                    await FileHelper.DeleteFileAsync(localFileModel);
                    Logger.Log("[FileMerger]", "Deleted local file since remoteFilePermission did no longer exist.");
                    return true;
                }
                if (localFileModel != null && localFileModel.Deleted) return true;
                Logger.Log("[FileMerger]", "Creating RemoteFilePermission since it did not exist");
                var (success, _) = await CloudAdapter.UploadFile(localFileModel);
                return success;
            }

            var remoteFileModel = await CloudAdapter.GetFile(remoteFilePermission.FileId);
            if (remoteFileModel == null) return false;

            Logger.Log("[FileMerger]", $"Merge -> Remote: {remoteFileModel.Name} ({remoteFileModel.LastChangedDate}) ({remoteFileModel.RemoteId})");

            if (localFileModel == null) {
                Logger.Log("[FileMerger]", "Creating LocalFileModel since it did not exist");
                if (!remoteFilePermission.Accepted) {
                    if (Invitations.Keys.Any(permission => permission.PermissionId == remoteFilePermission.PermissionId)) return true;
                    Invitations.Add(remoteFilePermission, remoteFileModel);
                    InvokeApprovalRequested();
                    return true;
                }

                if (Invitations.Keys.Any(permission => permission.PermissionId == remoteFilePermission.PermissionId)) {
                    Invitations.Remove(Invitations.Keys.FirstOrDefault(permission => permission.PermissionId == remoteFilePermission.PermissionId)!);
                    InvokeApprovalRequested();
                }

                localFileModel = FileModel.FromRemoteFileModel(remoteFileModel);
                Logger.Log(remoteFilePermission.UserPermission);
                if (remoteFilePermission.UserPermission == "2") {
                    localFileModel.Owner = remoteFilePermission.UserId;
                } else {
                    var permissionModels = await RemoteFileModel.GetRemotePermissionModels(localFileModel.RemoteId);
                    var remoteOwnerUid = permissionModels?.FirstOrDefault(pm => pm.UserPermission == "2")?.UserId;
                    localFileModel.Owner = remoteOwnerUid ?? "No Owner";
                    localFileModel.IsShared = true;
                }
                await FileHelper.CreateFileAsync(localFileModel);
            }

            if (localFileModel.Deleted) {
                Logger.Log("[FileMerger]", "Deleting file permission from server since it was deleted locally");
                await SynchronizationService.CloudApi.DeleteFilePermission(remoteFilePermission.PermissionId);
                await FileHelper.DeleteFileAsync(localFileModel, false);
                return true;
            }

            localFileModel = await MergeFileComponents(localFileModel, remoteFileModel);
            localFileModel = await MergeStrokeContent(localFileModel, remoteFileModel);

            await UpdateAll(localFileModel);

            if (localFileModel.Name != remoteFileModel.Name) {
                MainThread.BeginInvokeOnMainThread(() => MergeFileName(localFileModel, remoteFileModel));
                return false;
            }

            return true;
        }

        public static async Task<FileModel> MergeStrokeContent(FileModel localFileModel, RemoteFileModel remoteFileModel) {
            return await StrokeMerger.MergeStrokeContent(localFileModel, remoteFileModel);
        }

        public static async Task<FileModel> MergeFileComponents(FileModel localFileModel, RemoteFileModel remoteFileModel) {
            return await ComponentMerger.MergeComponents(localFileModel, remoteFileModel);
        }

        public static async void MergeFileName(FileModel localFileModel, RemoteFileModel remoteFileModel) {
            if (localFileModel == null || remoteFileModel == null || localFileModel.Name == remoteFileModel.Name || localFileModel.LastChangedDate < localFileModel.LastSynced) {
                if (localFileModel != null) CloudAdapter.ExcludedFiles.Add(localFileModel.Id);
                return;
            }

            CloudAdapter.ExcludedFiles.Add(localFileModel.Id);
            
            if (remoteFileModel.LastChangedDate > localFileModel.LastSynced && localFileModel.LastChangedDate != localFileModel.LastSynced) {
                var fileNameEntry = new MDEntry {
                    Text = localFileModel.Name
                };

                var localNameButton = new MDButton {
                    Text = "Local: " + localFileModel.Name,
                    Command = () => fileNameEntry.Text = localFileModel.Name
                };
                var remoteNameButton = new MDButton {
                    Text = "Cloud: " + remoteFileModel.Name,
                    Command = () => fileNameEntry.Text = remoteFileModel.Name
                };
            
                var content = new Frame {
                    Content = new StackPanel {
                        Width = 400,
                        Spacing = 10,
                        Children = {
                            fileNameEntry,
                            new StackPanel {
                                Spacing = 10,
                                Orientation = Orientation.Horizontal,
                                Children = {
                                    localNameButton,
                                    remoteNameButton
                                }
                            }
                        }
                    }
                };

                async void OkCallback() {
                    localFileModel.Name = fileNameEntry.Text;
                    await MainThread.InvokeOnMainThreadAsync(async () => await UpdateAll(localFileModel));
                }
                
                if (PopupNavigation.CurrentPopup == null) {
                    var popup = new MDContentPopup("File Name Conflict", content, OkCallback, closeWhenBackgroundIsClicked: false);
                    MainThread.BeginInvokeOnMainThread(() => {
                        PopupNavigation.OpenPopup(popup);
                    });
                }
                return;
            }

            if (remoteFileModel.LastChangedDate > localFileModel.LastChangedDate) {
                localFileModel.Name = remoteFileModel.Name;
                await FileHelper.UpdateFileAsync(localFileModel);
            } else {
                await UpdateAll(localFileModel);
            }
        }

        // update localFileModel as well as remoteFileModel
        private static async Task UpdateAll(FileModel fileModel) {
            Logger.Log("[FileMerger]", "Updating cloud and local database");
            await FileHelper.UpdateFileAsync(fileModel);
            await CloudAdapter.UpdateFile(fileModel);
            
            Logger.Log("[FileMerger]", "Updated cloud and local database");

            if (CloudAdapter.ExcludedFiles.Contains(fileModel.Id)) CloudAdapter.ExcludedFiles.Remove(fileModel.Id);
        }
    }
}