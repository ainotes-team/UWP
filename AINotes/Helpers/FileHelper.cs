using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using AINotes.Models;
using AINotes.Models.Enums;
using Helpers;
using Helpers.Extensions;
using Newtonsoft.Json;
using SQLite;

namespace AINotes.Helpers {
    public enum ChangeType {
        Created,
        Updated,
        Deleted,
    }

    public static class FileHelper {
        private static readonly SQLiteAsyncConnection FileDatabase = new SQLiteAsyncConnection(LocalFileHelper.ToAbsolutePath("database.db3"));
        public static event Action<FileModel, ChangeType> FileChanged;
        public static event Action<DirectoryModel, ChangeType> DirectoryChanged;

        static FileHelper() {
            FileDatabase.CreateTableAsync<FileModel>().Wait();
            FileDatabase.CreateTableAsync<ComponentModel>().Wait();
            FileDatabase.CreateTableAsync<DirectoryModel>().Wait();
            var labelTableCreated = FileDatabase.CreateTableAsync<LabelModel>().Result;
            // ReSharper disable once InvertIf
            if (labelTableCreated == CreateTableResult.Created) {
                Logger.Log("[FileHelper]", "Creating default labels");
                var colors = new Dictionary<string, Color> {
                    {"none", ColorCreator.FromHex("#FFFFFF")},
                    {"maths", ColorCreator.FromHex("#FCB450")},
                    {"german", ColorCreator.FromHex("#5851DB")},
                    {"english", ColorCreator.FromHex("#5CBC63")},
                    {"latin", ColorCreator.FromHex("#FC5650")},
                    {"french", ColorCreator.FromHex("#E56588")},
                    {"physics", ColorCreator.FromHex("#5D9EBE")},
                    {"biology", ColorCreator.FromHex("#96C03A")},
                    {"chemistry", ColorCreator.FromHex("#963AC0")},
                    {"informatics", ColorCreator.FromHex("#898F8F")},
                    {"history", ColorCreator.FromHex("#AE8B65")},
                    {"socialstudies", ColorCreator.FromHex("#B0DDF0")},
                    {"geography", ColorCreator.FromHex("#FAC9A1")},
                    {"religion", ColorCreator.FromHex("#4E5357")},
                    {"philosophy", ColorCreator.FromHex("#7D82BA")},
                    {"music", ColorCreator.FromHex("#FEF2B6")},
                    {"art", ColorCreator.FromHex("#FAE355")},
                };
                var labels = colors.Keys.Select(itm => new LabelModel {
                    Color = colors[itm],
                    Name = itm
                });
                FileDatabase.InsertAllAsync(labels);
            }
        }

        public static async Task ClearDatabase() {
            await FileDatabase.DeleteAllAsync(new TableMapping(typeof(FileModel)));
            await FileDatabase.DeleteAllAsync(new TableMapping(typeof(DirectoryModel)));
            await FileDatabase.DeleteAllAsync(new TableMapping(typeof(ComponentModel)));
        }

        public static async Task<int> CreateFileAsync(FileModel fm) {
            var fileModel = new FileModel {
                Name = fm.Name,
                Subject = fm.Subject,
                ParentDirectoryId = fm.ParentDirectoryId,
                CreationDate = fm.CreationDate,
                LastChangedDate = fm.LastChangedDate,
                LastSynced = fm.LastSynced,
                StrokeContent = fm.StrokeContent,
                LineMode = fm.LineMode,
                RemoteId = fm.RemoteId,
                Owner = fm.Owner,
                IsShared = fm.IsShared
            };
            await FileDatabase.InsertAsync(fileModel);
            FileChanged?.Invoke(fileModel, ChangeType.Created);
            return fileModel.FileId;
        }

        public static async Task<int> CreateFileAsync(string name, string subject, int parentDirectoryId, List<int> labelIds=null, string owner=null) {
            var currentTime = Time.CurrentTimeMillis();
            var fileModel = new FileModel {
                Name = name,
                Subject = subject,
                ParentDirectoryId = parentDirectoryId,
                CreationDate = currentTime,
                LastChangedDate = currentTime,
                StrokeContent = null,
                LabelsString = (labelIds ?? new List<int>()).Serialize(),
                // LineMode = Preferences.BackgroundDefaultLineMode,
                Owner = owner,
            };
            await FileDatabase.InsertAsync(fileModel);
            FileChanged?.Invoke(fileModel, ChangeType.Created);
            return fileModel.FileId;
        }

        public static async Task UpdateFileAsync(FileModel fm) {
            var fileModel = new FileModel {
                FileId = fm.FileId,
                Name = fm.Name,
                Subject = fm.Subject,
                ParentDirectoryId = fm.ParentDirectoryId,
                CreationDate = fm.CreationDate,
                LastChangedDate = fm.LastChangedDate,
                LastSynced = fm.LastSynced,
                StrokeContent = fm.StrokeContent,
                LineMode = fm.LineMode,
                RemoteId = fm.RemoteId,
                Owner = fm.Owner,
                IsShared = fm.IsShared,
                IsFavorite = fm.IsFavorite,
                LabelsString = fm.LabelsString
            };
            
            await FileDatabase.UpdateAsync(fileModel);
            FileChanged?.Invoke(fileModel, ChangeType.Updated);
        }

        public static async Task UpdateFileAsync(int fileId,string name = null, string subject = null, int parentDirectoryId = -1, string strokeContent = null, DocumentLineMode lineMode = DocumentLineMode.GridMedium, long lastSynced = 0, float zoom = 1, double scrollX = 0, double scrollY = 0, List<int> labelIds=null, double? width=null, double? height=null) {
            var currentTime = Time.CurrentTimeMillis();
            var fileModel = await FileDatabase.Table<FileModel>().Where(c => c.FileId == fileId).FirstAsync();

            if (name != null && fileModel.Name != name) {
                fileModel.Name = name;
            }

            if (subject != null && fileModel.Subject != subject) {
                fileModel.Subject = subject;
            }

            if (parentDirectoryId != -1 && fileModel.ParentDirectoryId != parentDirectoryId) {
                fileModel.ParentDirectoryId = parentDirectoryId;
            }

            if (strokeContent != null && fileModel.StrokeContent != strokeContent) {
                fileModel.StrokeContent = strokeContent;
            }

            // if (fileModel.LineMode != lineMode) {
            //     fileModel.LineMode = lineMode;
            // }

            if (labelIds != null) {
                fileModel.LabelsString = labelIds.Serialize();
            }

            fileModel.Zoom = zoom;
            fileModel.ScrollX = scrollX;
            fileModel.ScrollY = scrollY;
            fileModel.Width = width;
            fileModel.Height = height;

            fileModel.LastChangedDate = currentTime;

            await FileDatabase.UpdateAsync(fileModel);
            FileChanged?.Invoke(fileModel, ChangeType.Updated);
        }

        public static async Task<FileModel> GetFileAsync(int fileId) {
            return await FileDatabase.Table<FileModel>().Where(c => c.FileId == fileId).FirstAsync();
        }

        public static async Task DeleteFileAsync(FileModel fileModel, bool markOnly = true) {
            if (fileModel == null) return;
            await DeleteFileComponentsAsync(fileModel.FileId);
            if (markOnly) {
                await FileDatabase.ExecuteAsync("UPDATE FileModel SET Deleted = ? WHERE FileId = ?", 1, fileModel.FileId);
                FileChanged?.Invoke(fileModel, ChangeType.Deleted);
            } else {
                await FileDatabase.DeleteAsync(fileModel);
                if (!fileModel.Deleted) {
                    fileModel.Deleted = true;
                    FileChanged?.Invoke(fileModel, ChangeType.Deleted);
                }
            }
        }

        public static async Task<int> CreateComponentAsync(ComponentModel componentModel) {
            await FileDatabase.InsertAsync(componentModel);
            return componentModel.ComponentId;
        }

        public static async Task<int> UpdateComponentAsync(ComponentModel componentModel) {
            return await FileDatabase.UpdateAsync(componentModel);
        }

        public static async Task<ComponentModel> GetComponentByRemoteIdAsync(string remoteComponentId) {
            return await FileDatabase.Table<ComponentModel>().Where(itm => itm.RemoteId == remoteComponentId).FirstOrDefaultAsync();
        }

        public static async Task<int> DeleteComponentAsync(ComponentModel componentModel) {
            return await FileDatabase.DeleteAsync(componentModel);
        }
        
        public static async Task<ComponentModel> GetComponentAsync(int componentId) {
            var componentsList = FileDatabase.Table<ComponentModel>();
            if (await componentsList.CountAsync() == 0) return null;
            return await componentsList.FirstOrDefaultAsync(model => model.ComponentId == componentId);
        }

        public static async Task<List<ComponentModel>> GetFileComponentsAsync(int fileId) {
            return await FileDatabase.Table<ComponentModel>().Where(c => c.FileId == fileId).ToListAsync();
        }

        public static async Task DeleteFileComponentsAsync(int fileId) {
            var componentModels = await FileDatabase.Table<ComponentModel>().Where(c => c.FileId == fileId).ToListAsync();
            var tasks = componentModels.Select(componentModel => FileDatabase.DeleteAsync(componentModel)).ToList();
            await Task.WhenAll(tasks);
        }

        public static async Task<int> CreateDirectoryAsync(string name, int parentDirectoryId) {
            var directoryModel = new DirectoryModel {
                Name = name,
                ParentDirectoryId = parentDirectoryId
            };
            return await CreateDirectoryAsync(directoryModel);
        }

        public static async Task<int> CreateDirectoryAsync(DirectoryModel directoryModel) {
            await FileDatabase.InsertAsync(directoryModel);
            DirectoryChanged?.Invoke(directoryModel, ChangeType.Created);

            return directoryModel.DirectoryId;
        }

        public static async Task UpdateDirectoryAsync(int directoryId, string directoryName, int? parentDirectoryId=null) {
            var currentTime = Time.CurrentTimeMillis();
            var directoryModel = await FileDatabase.Table<DirectoryModel>().Where(c => c.DirectoryId == directoryId).FirstAsync();

            if (directoryName != null && directoryModel.Name != directoryName) {
                directoryModel.Name = directoryName;
            }

            if (parentDirectoryId != null) {
                directoryModel.ParentDirectoryId = (int) parentDirectoryId;
            }

            directoryModel.LastChangedDate = currentTime;

            await FileDatabase.UpdateAsync(directoryModel);
            DirectoryChanged?.Invoke(directoryModel, ChangeType.Updated);
        }

        public static async Task<DirectoryModel> GetDirectoryAsync(int directoryId) {
            if (directoryId == 0) {
                return new DirectoryModel {
                    DirectoryId = 0
                };
            }

            return await FileDatabase.Table<DirectoryModel>().Where(c => c.DirectoryId == directoryId).FirstAsync();
        }

        public static async Task DeleteDirectoryAsync(DirectoryModel directoryModel) {
            // delete content
            var containedFiles = await ListFilesReducedAsync(directoryModel.DirectoryId);
            var containedDirectories = await ListDirectoriesAsync(directoryModel.DirectoryId);

            foreach (var file in containedFiles) {
                await DeleteFileAsync(file);
            }

            foreach (var directory in containedDirectories) {
                await DeleteDirectoryAsync(directory.DirectoryId);
            }

            // delete model
            await FileDatabase.DeleteAsync(directoryModel);
            DirectoryChanged?.Invoke(directoryModel, ChangeType.Deleted);
        }

        public static async Task DeleteDirectoryAsync(int directoryId) {
            var directoryModel = await FileDatabase.Table<DirectoryModel>().Where(c => c.DirectoryId == directoryId).FirstAsync();
            await DeleteDirectoryAsync(directoryModel);
        }

        public static List<FileModel> ListFiles(int parentDirectoryId, bool includeDeleted = false) {
            if (parentDirectoryId == -1) {
                return FileDatabase.GetConnection().Table<FileModel>().Where(c => c.Deleted != true || includeDeleted).ToList();
            }

            var tableQuery = FileDatabase.GetConnection().Table<FileModel>().Where(c => c.ParentDirectoryId == parentDirectoryId && (c.Deleted != true || includeDeleted));
            var result = tableQuery.ToList();

            return result;
        }

        public static async Task<List<FileModel>> ListFilesReducedAsync(int parentDirectoryId = -1, bool includeDeleted = false) {
            const string baseQuery = "select FileId, ParentDirectoryId, Name, Subject, CreationDate, LastChangedDate, LineMode, LastSynced, Deleted, IsFavorite, IsShared, LabelsString, RemoteId, Owner from FileModel ";
            if (!includeDeleted) {
                if (parentDirectoryId == -1) {
                    return await FileDatabase.QueryAsync<FileModel>(baseQuery + "where Deleted = 0");
                }

                return await FileDatabase.QueryAsync<FileModel>(baseQuery + $"where Deleted = 0 and ParentDirectoryId = {parentDirectoryId}");
            }
            
            if (parentDirectoryId == -1) {
                return await FileDatabase.QueryAsync<FileModel>(baseQuery);
            }

            return await FileDatabase.QueryAsync<FileModel>(baseQuery + $"where ParentDirectoryId = {parentDirectoryId}");            
        }

        public static async Task<List<FileModel>> ListFilesAsync(int parentDirectoryId = -1, bool includeDeleted = false) {
            if (parentDirectoryId == -1) {
                return await FileDatabase.Table<FileModel>().Where(c => c.Deleted != true || includeDeleted).ToListAsync();
            }

            var tableQuery = FileDatabase.Table<FileModel>().Where(c => c.ParentDirectoryId == parentDirectoryId && (c.Deleted != true || includeDeleted));

            var result = await tableQuery.ToListAsync();
            return result;
        }

        public static List<DirectoryModel> ListDirectories(int parentDirectoryId = -1) {
            if (parentDirectoryId == -1) {
                return FileDatabase.GetConnection().Table<DirectoryModel>().ToList();
            }
            var tableQuery = FileDatabase.GetConnection().Table<DirectoryModel>().Where(c => c.ParentDirectoryId == parentDirectoryId);
            var result = tableQuery.ToList();
            return result;
        }

        public static async Task<List<DirectoryModel>> ListDirectoriesAsync(int parentDirectoryId = -1) {
            if (parentDirectoryId == -1) {
                return await FileDatabase.Table<DirectoryModel>().ToListAsync();
            }

            return await FileDatabase.Table<DirectoryModel>().Where(c => c.ParentDirectoryId == parentDirectoryId).ToListAsync();
        }

        public static async Task<int> CleanDatabaseAsync(Action<int> progressCallback = null) {
            var allFiles = await ListFilesReducedAsync(includeDeleted: true);
            var deletedFileCounter = 0;
            foreach (var file in allFiles.Where(fm => fm.Deleted)) {
                await DeleteFileAsync(file, false);
                deletedFileCounter += 1;
                progressCallback?.Invoke(deletedFileCounter);
            }

            await FileDatabase.ExecuteAsync("VACUUM;");

            return deletedFileCounter;
        }

        public static async Task<string> GetFileJsonAsync(FileModel fileModel) {
            var fileComponentModels = await GetFileComponentsAsync(fileModel.FileId);
            var modifiedFileComponentModels = new List<ComponentModel>();
            foreach (var componentModel in fileComponentModels) {
                if (componentModel.Type == "ImageComponent") {
                    var imagePath = componentModel.Content;
                    using (var imageFile = File.OpenRead(LocalFileHelper.ToAbsolutePath(imagePath))) {
                        var imageBytes = imageFile.ReadAllBytes();
                        componentModel.Content = JsonConvert.SerializeObject(imageBytes);
                    }
                }
                
                modifiedFileComponentModels.Add(componentModel);
            }
            
            fileModel.InternalComponentModels = JsonConvert.SerializeObject(modifiedFileComponentModels);
            return JsonConvert.SerializeObject(fileModel);
        }

        public static async Task SetRemoteId(FileModel fileModel, string remoteId) {
            await FileDatabase.ExecuteAsync("UPDATE FileModel SET RemoteId = ? WHERE FileId = ?", remoteId, fileModel.FileId); 
            await FileDatabase.ExecuteAsync("UPDATE FileModel SET LastChangedDate = ? WHERE FileId = ?", Time.CurrentTimeMillis(), fileModel.FileId);
        }
        
        public static async Task SetRemoteId(ComponentModel componentModel, string remoteId) {
            componentModel.RemoteId = remoteId;
            await FileDatabase.UpdateAsync(componentModel);
        }

        public static async Task SetLastSynced(FileModel fileModel) {
            await FileDatabase.ExecuteAsync("UPDATE FileModel SET LastSynced = ? WHERE FileId = ?", fileModel.LastChangedDate, fileModel.FileId);
        }
        
        public static async Task SetLastSynced(ComponentModel componentModel) {
            await FileDatabase.ExecuteAsync("UPDATE PluginModel SET LastSynced = ? WHERE PluginId = ?", componentModel.LastSynced, componentModel.ComponentId);
        }
        
        public static async Task SetRemoteAccount(FileModel fileModel, string remoteAccountId) {
            await FileDatabase.ExecuteAsync("UPDATE FileModel SET RemoteAccountId = ? WHERE FileId = ?", remoteAccountId, fileModel.FileId);
        }

        public static async Task SetFavorite(FileModel fileModel, bool favorite) {
            await FileDatabase.ExecuteAsync("UPDATE FileModel SET IsFavorite = ? WHERE FileId = ?", favorite, fileModel.FileId);
        }

        public static async Task SetShared(int fileId, bool shared) {
            await FileDatabase.ExecuteAsync("UPDATE FileModel SET IsShared = ? WHERE FileId = ?", shared, fileId);
        }

        public static async Task<int> GetLocalFileFromRemoteId(string remoteId) {
            return (await (FileDatabase.Table<FileModel>().Where(model => model.RemoteId == remoteId)).FirstAsync()).Id;
        }
        
        public static async Task SetDeleted(int componentId) {
            await FileDatabase.ExecuteAsync("UPDATE PluginModel SET Deleted = ? WHERE PluginId = ?", true, componentId);
            await FileDatabase.ExecuteAsync("UPDATE PluginModel SET DeletionLastChanged = ? WHERE PluginId = ?", Time.CurrentTimeMillis(), componentId);
        }

        public static async Task<int> CreateLabelAsync(LabelModel labelModel) {
            var model = new LabelModel {
                Name = labelModel.Name,
                HexColor = labelModel.HexColor,
            };
            await FileDatabase.InsertAsync(model);
            return model.LabelId;
        }

        public static async Task UpdateLabelAsync(LabelModel labelModel) {
            await FileDatabase.UpdateAsync(labelModel);
        }

        public static async Task<LabelModel> GetLabelAsync(int labelId) {
            return await FileDatabase.Table<LabelModel>().Where(c => c.LabelId == labelId).FirstAsync();
        }

        public static async Task<List<LabelModel>> ListLabelsAsync() {
            return await FileDatabase.Table<LabelModel>().ToListAsync();
        }
        
        public static async Task DeleteLabelAsync(LabelModel labelModel) {
            foreach (var fm in await ListFilesReducedAsync(-1, true)) {
                if (!fm.Labels.Contains(labelModel.LabelId)) continue;
                var ls = fm.Labels;
                ls.Remove(labelModel.LabelId);
                await UpdateFileAsync(fm.FileId, fm.Name, labelIds: ls.ToList());
            }
            await FileDatabase.DeleteAsync(labelModel);
        }

        // public static async Task SetFileLabels(int fileId, IEnumerable<LabelModel> selectedLabels) {
        //     await FileDatabase.ExecuteAsync("UPDATE FileModel SET LabelsString = ? WHERE FileId = ?", selectedLabels.Select(itm => itm.LabelId).Serialize(), fileId);
        //     FileChanged?.Invoke(await GetFileAsync(fileId), ChangeType.Updated);
        // }

        public static async Task SetFileBookmarks(int fileId, IList<FileBookmarkModel> bookmarks) {
            Logger.Log("SetFileBookmarks", fileId, bookmarks.ToFString());
            await FileDatabase.ExecuteAsync("UPDATE FileModel SET SerializedBookmarks = ? WHERE FileId = ?", bookmarks.Serialize(), fileId);
        }
    }
}