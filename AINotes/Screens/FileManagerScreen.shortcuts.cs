using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AINotes.Controls.Popups;
using AINotes.Helpers;
using AINotes.Models;
using Helpers;
using Helpers.Essentials;

namespace AINotes.Screens {
    public partial class FileManagerScreen {
        private bool DeleteShortcut() {
            if (App.Page.Content != this) return false;
            if (PopupNavigation.CurrentPopup != null) return false;
            foreach (var fileItem in FileContainer.SelectedModels.Where(itm => itm is FileModel).Cast<FileModel>().ToList()) {
                Task.Run(async () => {
                    await FileHelper.DeleteFileAsync(fileItem);
                });
            }

            foreach (var directoryItem in FileContainer.SelectedModels.Where(itm => itm is DirectoryModel).Cast<DirectoryModel>().ToList()) {
                Task.Run(async () => {
                    await FileHelper.DeleteDirectoryAsync(directoryItem);
                });
            }

            return true;
        }

        private bool RenameShortcut() {
            if (App.Page.Content != this) return false;
            if (PopupNavigation.CurrentPopup != null) return false;
            if (FileContainer.SelectedModels.Count != 1) return false;
            if (FileContainer.SelectedModels.Count(itm => itm is FileModel) == 1) {
                // rename file
                var fileItem = (FileModel) FileContainer.SelectedModels.First(itm => itm is FileModel);
                OpenFileRenamePopup(fileItem);
            } else {
                // rename directory
                var directoryItem = (DirectoryModel) FileContainer.SelectedModels.First(itm => itm is DirectoryModel);
                OpenDirectoryRenamePopup(directoryItem);
            }

            return true;
        }

        private readonly List<FileModel> _copiedFileModels = new List<FileModel>();
        private readonly List<FileModel> _cutFileModels = new List<FileModel>();
        private readonly List<DirectoryModel> _copiedDirectoryModels = new List<DirectoryModel>();
        private readonly List<DirectoryModel> _cutDirectoryModels = new List<DirectoryModel>();

        private bool PasteShortcut() {
            if (App.Page.Content != this) return false;
            if (PopupNavigation.CurrentPopup != null) return false;
            var targetDirectoryId = App.FileManagerScreen.CurrentDirectory.DirectoryId;

            Task.Run(async () => {
                foreach (var reducedFileModel in _copiedFileModels) {
                    var labels = new List<int>(reducedFileModel.Labels);
                    var newFileId = await FileHelper.CreateFileAsync(reducedFileModel.Name, reducedFileModel.Subject, targetDirectoryId, labels);
                    await FileHelper.UpdateFileAsync(newFileId, strokeContent: (await FileHelper.GetFileAsync(reducedFileModel.Id)).StrokeContent);
                    var currentComponentModels = await reducedFileModel.GetComponentModels();
                    foreach (var componentModel in currentComponentModels) {
                        await FileHelper.CreateComponentAsync(new ComponentModel {
                            FileId = newFileId,
                            Type = componentModel.Type,
                            Content = componentModel.Content,
                            Position = componentModel.Position,
                            Size = componentModel.Size,
                            Deleted = componentModel.Deleted
                        });
                    }
                }

                foreach (var directoryModel in _copiedDirectoryModels) {
                    await FileHelper.CreateDirectoryAsync(directoryModel.Name, directoryModel.ParentDirectoryId);
                }

                foreach (var fileModel in _cutFileModels) {
                    await FileHelper.UpdateFileAsync(fileModel.FileId, parentDirectoryId: targetDirectoryId);
                }

                foreach (var directoryModel in _cutDirectoryModels) {
                    await FileHelper.UpdateDirectoryAsync(directoryModel.DirectoryId, directoryModel.Name, targetDirectoryId);
                }
                
                MainThread.BeginInvokeOnMainThread(() => {
                    _cutFileModels.Clear();
                    _cutDirectoryModels.Clear();
            
                    App.FileManagerScreen.LoadFiles();
                });
            });
            
            return true;
        }

        private bool CutShortcut() {
            if (App.Page.Content != this) return false;
            if (PopupNavigation.CurrentPopup != null) return false;
            
            _copiedFileModels.Clear();
            _copiedDirectoryModels.Clear();
            _cutFileModels.Clear();
            _cutDirectoryModels.Clear();
            
            _cutFileModels.AddRange(FileContainer.SelectedModels.Where(m => m is FileModel).Cast<FileModel>());
            _cutDirectoryModels.AddRange(FileContainer.SelectedModels.Where(m => m is DirectoryModel).Cast<DirectoryModel>());
            return true;
        }

        private bool CopyShortcut() {
            if (App.Page.Content != this) return false;
            if (PopupNavigation.CurrentPopup != null) return false;
            
            _copiedFileModels.Clear();
            _copiedDirectoryModels.Clear();
            _cutFileModels.Clear();
            _cutDirectoryModels.Clear();
            
            _copiedFileModels.AddRange(FileContainer.SelectedModels.Where(m => m is FileModel).Cast<FileModel>());
            _copiedDirectoryModels.AddRange(FileContainer.SelectedModels.Where(m => m is DirectoryModel).Cast<DirectoryModel>());
            return true;
        }

        private bool SelectAllShortcut() {
            if (App.Page.Content != this) return false;
            if (PopupNavigation.CurrentPopup != null) return false;
            Logger.Log("[FileManagerScreen]", "Shortcuts: SelectAllShortcut - FileContainer.SelectAll");
            FileContainer.SelectAll();
            return true;
        }
        
        private bool CreateDirectoryShortcut() {
            if (App.Page.Content != this) return false;
            if (PopupNavigation.CurrentPopup != null) return false;
            OpenFolderCreationPopup(CurrentDirectory.DirectoryId);
            return true;
        }

        private bool CreateFileShortcut() {
            if (App.Page.Content != this) return false;
            if (PopupNavigation.CurrentPopup != null) return false;
            OpenFileCreationPopup(CurrentDirectory.DirectoryId);
            return true;
        }

        private bool SearchShortcut() {
            if (App.Page.Content != this) return false;
            if (PopupNavigation.CurrentPopup != null) return false;
            App.Page.LeftSidebar.SendItemPress(0);
            return true;
        }
        
        private bool ReloadShortcut() {
            if (App.Page.Content != this) return false;
            if (PopupNavigation.CurrentPopup != null) return false;
            LoadFiles(true);
            return true;
        }
        
        private void OnEnterPressed() {
            if (App.Page.Content != this) return;
            if (PopupNavigation.CurrentPopup != null) return;
            if (FileContainer.SelectedModels.Count != 1) return;
            switch (FileContainer.SelectedModels.First()) {
                case FileModel fm:
                    FileContainer.OpenFile(fm);
                    break;
                case DirectoryModel dm:
                    FileContainer.OpenDirectory(dm);
                    break;
            }
        }

        private void LoadShortcuts() {
            Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.SelectAllShortcut, "fms_selectAll", SelectAllShortcut));
            Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.DeleteShortcut, "fms_delete", DeleteShortcut));
            Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.RenameShortcut, "fms_rename", RenameShortcut));
            
            Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.CopyShortcut, "fms_copy", CopyShortcut));
            Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.CutShortcut, "fms_cut", CutShortcut));
            Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.PasteShortcut, "fms_paste", PasteShortcut));
            
            Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.CreateDirectoryShortcut, "fms_createFolder", CreateDirectoryShortcut, true));
            Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.CreateFileShortcut, "fms_createFile", CreateFileShortcut, true));
            
            Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.SearchShortcut, "fms_search", SearchShortcut, true));
            
            Shortcuts.AddShortcut(new ShortcutModel(() => Preferences.ReloadShortcut, "fms_reload", ReloadShortcut));

            Shortcuts.EnterPressed += OnEnterPressed;
        }
    }
}