using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AINotes.Helpers;
using Helpers;
using Helpers.Essentials;
using AINotes.Models;

namespace AINotes.Screens {
    public partial class FileManagerScreen {
        // filter / sort
        public List<int> CurrentFilterLabels;
        private Func<(Dictionary<int, FileModel>, Dictionary<int, DirectoryModel>), (Dictionary<int, FileModel>, Dictionary<int, DirectoryModel>)> _currentFilterCommand;

        // states
        private DirectoryModel _currentDirectory;

        public DirectoryModel CurrentDirectory {
            get {
                if (_currentDirectory == null) {
                    return new DirectoryModel {
                        DirectoryId = 0
                    };
                }

                return _currentDirectory;
            }
            set => _currentDirectory = value;
        }

        public static bool LoadingDirectories;
        public static bool LoadingFiles;

        public async void LoadFiles(bool force=false) {
            if (LoadingFiles) return;
            // if (App.Page?.Content != App.FileManagerScreen) return;
            LoadingFiles = true;
            Logger.Log("[FileManagerScreen]", "-> LoadFiles", logLevel: LogLevel.Debug);

            // load files
            var files = await FileHelper.ListFilesReducedAsync(CurrentDirectory.DirectoryId);
            Logger.Log("[FileManagerScreen]", "LoadFiles:", $"Loaded {files.Count} files", logLevel: LogLevel.Verbose);

            // get old and new Ids
            var oldIds = FileContainer.ModelCollection.Where(it => it is FileModel).ToDictionary(model => ((FileModel) model).FileId);
            var newIds = files.ToDictionary(model => model.FileId);

            // filter newIds
            if (_currentFilterCommand != null) {
                (newIds, _) = _currentFilterCommand((newIds, null));
            }

            MainThread.BeginInvokeOnMainThread(() => {
                Logger.Log("[FileManagerScreen]", "-> LoadFiles: Main", logLevel: LogLevel.Debug);

                // check if file exists -> update or add
                Logger.Log("[FileManagerScreen]", $"LoadFiles: Checking for file updates {oldIds.Count}->{newIds.Count}", logLevel: LogLevel.Verbose);
                foreach (var (fileId, fileModel) in newIds) {
                    if (oldIds.ContainsKey(fileId)) {
                        // check if changed
                        if (!force) {
                            if (oldIds[fileId].LastChangedDate == fileModel.LastChangedDate) {
                                if (oldIds[fileId].Status == fileModel.Status) {
                                    continue;
                                }
                            }
                        }

                        // update old files
                        Logger.Log("[FileManagerScreen]", "LoadFiles: Updated", fileModel.Name);
                        FileContainer.ModelCollection.Insert(FileContainer.ModelCollection.IndexOf(oldIds[fileId]), fileModel);
                        FileContainer.ModelCollection.Remove(oldIds[fileId]);
                    } else {
                        // add new files
                        // Logger.Log("[FileManagerScreen]", "LoadFiles: Added", fileModel.Name);
                        FileContainer.ModelCollection.Add(fileModel);
                    }
                }

                // remove deleted files ( => oldIds that are not in newIds )
                Logger.Log("[FileManagerScreen]", "LoadFiles: Checking for file deletions", logLevel: LogLevel.Verbose);
                foreach (var (fileId, fileModel) in oldIds) {
                    if (newIds.ContainsKey(fileId)) continue;
                    // Logger.Log("[FileManagerScreen]", "LoadFiles: Removed", fileModel.Name);
                    FileContainer.ModelCollection.Remove(fileModel);
                }

                LoadingFiles = false;
                Logger.Log("[FileManagerScreen]", "<- LoadFiles: Main", logLevel: LogLevel.Debug);
            });
            
            Logger.Log("[FileManagerScreen]", "<- LoadFiles", logLevel: LogLevel.Verbose);
        }


        public async void LoadDirectories() {
            if (LoadingDirectories) return;
            if (App.Page.Content != App.FileManagerScreen) return;
            LoadingDirectories = true;
            Logger.Log("[FileManagerScreen]", "-> LoadDirectories", logLevel: LogLevel.Verbose);

            // load directories
            var directories = await FileHelper.ListDirectoriesAsync(CurrentDirectory.DirectoryId);

            var oldIds = FileContainer.ModelCollection.Where(itm => itm is DirectoryModel).ToDictionary(model => ((DirectoryModel) model).DirectoryId);
            var newIds = directories.ToDictionary(model => model.DirectoryId);

            MainThread.BeginInvokeOnMainThread(() => {
                Logger.Log("[FileManagerScreen]", "-> LoadDirectories: Main", logLevel: LogLevel.Debug);

                foreach (var (directoryId, directoryModel) in newIds) {
                    if (oldIds.ContainsKey(directoryId)) {
                        // Logger.Log("[FileManagerScreen]", "LoadDirectories: Updated", directoryModel.Name);
                        FileContainer.ModelCollection.Insert(FileContainer.ModelCollection.IndexOf(oldIds[directoryId]), directoryModel);
                        FileContainer.ModelCollection.Remove(oldIds[directoryId]);
                    } else {
                        // Logger.Log("[FileManagerScreen]", "LoadDirectories: Added", directoryModel.Name);
                        FileContainer.ModelCollection.Add(directoryModel);
                    }
                }

                foreach (var (directoryId, directoryModel) in oldIds) {
                    if (newIds.ContainsKey(directoryId)) continue;
                    // Logger.Log("[FileManagerScreen]", "LoadDirectories: Removed", directoryModel.Name);
                    FileContainer.ModelCollection.Remove(directoryModel);
                }

                LoadingDirectories = false;
                Logger.Log("[FileManagerScreen]", "<- LoadDirectories: Main", logLevel: LogLevel.Debug);
            });
            Logger.Log("[FileManagerScreen]", "<- LoadDirectories", logLevel: LogLevel.Verbose);
        }

        private async Task ResetFilter() {
            CurrentFilterLabels = null;
            await LoadFilterChips();
        }
    }
}