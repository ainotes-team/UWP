using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using AINotes.Components;
using AINotes.Components.Implementations;
using AINotes.Models;
using AINotesCloud;
using AINotesCloud.Models;
using Helpers;
using Helpers.Essentials;
using Microsoft.Graph;
using File = System.IO.File;

namespace AINotes.Helpers.Merging {
    public static class ComponentMerger {
        
        public static async Task<FileModel> MergeComponents(FileModel localFileModel, RemoteFileModel remoteFileModel) {
            var matchedComponents = MatchComponents(await localFileModel.GetComponentModels(), await remoteFileModel.GetRemoteComponentModels());
            
            foreach (var (localComponentModel, remoteComponentModel) in matchedComponents) {
                if (localComponentModel != null) {
                    if (localComponentModel.FileId == App.EditorScreen.FileId) {
                        MergeComponentEditor(localComponentModel, remoteComponentModel);
                    } else {
                        await MergeComponentDatabase(localComponentModel, remoteComponentModel);
                    }
                } else if (remoteComponentModel != null) {
                    if ((await ComponentModel.FromRemoteComponentModelAsync(remoteComponentModel)).FileId == App.EditorScreen.FileId) {
                        MergeComponentEditor(null, remoteComponentModel);
                    } else {
                        await MergeComponentDatabase(null, remoteComponentModel);
                    }
                } else {
                    Logger.Log("Error merging components: Both localComponentModel as well as remoteComponentModel are null");
                }
            }

            return localFileModel;
        }

        private static async Task<string> CreateRemoteComponentModel(ComponentModel localComponentModel) {
            var rcm = await localComponentModel.ToRemoteComponentModelAsync();
            Logger.Log("Type:", rcm.Type);

            switch (rcm.Type) {
                case "ImageComponent":
                    if (rcm.Content == null) return null;

                    var imagePath = rcm.Content;
                    var storageFile = await StorageFile.GetFileFromPathAsync(LocalFileHelper.ToAbsolutePath(imagePath));
                    var stream = await storageFile.OpenAsync(FileAccessMode.Read);

                    var imageId = await SynchronizationService.CloudApi.UploadImage(stream.AsStream(), rcm.RemoteFileId);

                    if (imageId == null) return null;
                    rcm.Content = imageId;

                    break;
            }

            var (_, remoteId) = await SynchronizationService.CloudApi.PostComponent(rcm);

            return remoteId;
        }
        
        private static async Task<ComponentModel> CreateLocalComponentModel(RemoteComponentModel remoteComponentModel) {
            var componentModel = await ComponentModel.FromRemoteComponentModelAsync(remoteComponentModel);

            var id = await FileHelper.CreateComponentAsync(componentModel);

            switch (componentModel.Type) {
                case "ImageComponent":
                    var savingPath = ImageComponent.GetImageSavingPathStatic(id);
                    await SynchronizationService.CloudApi.DownloadImage(componentModel.Content, savingPath);
                    componentModel.ComponentId = id;
                    componentModel.Content = savingPath;
                    await FileHelper.UpdateComponentAsync(componentModel);

                    break;
            }

            return componentModel;
        }
        
        public static async Task<(ComponentModel, RemoteComponentModel)> CompareComponentModels(ComponentModel localComponentModel, RemoteComponentModel remoteComponentModel) {
            if (localComponentModel.Content == remoteComponentModel.Content && localComponentModel.Rectangle.Equals(remoteComponentModel.Rectangle) && localComponentModel.ZIndex.Equals(remoteComponentModel.ZIndex)) return (localComponentModel, remoteComponentModel);

            if (localComponentModel.PositionLastChanged < remoteComponentModel.PositionLastUpdated) {
                localComponentModel.PosX = remoteComponentModel.Rectangle[0];
                localComponentModel.PosY = remoteComponentModel.Rectangle[1];
                localComponentModel.PositionLastChanged = remoteComponentModel.PositionLastUpdated;
            } else {
                remoteComponentModel.Rectangle[0] = localComponentModel.PosX;
                remoteComponentModel.Rectangle[1] = localComponentModel.PosY;
                remoteComponentModel.PositionLastUpdated = localComponentModel.PositionLastChanged;
            }

            if (localComponentModel.SizeLastChanged < remoteComponentModel.SizeLastUpdated) {
                localComponentModel.SizeX = remoteComponentModel.Rectangle[2];
                localComponentModel.SizeY = remoteComponentModel.Rectangle[3];
                localComponentModel.SizeLastChanged = remoteComponentModel.SizeLastUpdated;
            } else {
                remoteComponentModel.Rectangle[2] = localComponentModel.SizeX;
                remoteComponentModel.Rectangle[3] = localComponentModel.SizeY;
                remoteComponentModel.SizeLastUpdated = localComponentModel.SizeLastChanged;
            }

            if (localComponentModel.ZIndexLastChanged < remoteComponentModel.ZIndexLastUpdated) {
                localComponentModel.ZIndex = remoteComponentModel.ZIndex;
                localComponentModel.ZIndexLastChanged = remoteComponentModel.ZIndexLastUpdated;
            } else {
                remoteComponentModel.ZIndex = localComponentModel.ZIndex;
                remoteComponentModel.ZIndexLastUpdated = localComponentModel.ZIndexLastChanged;
            }

            if (localComponentModel.ContentLastChanged < remoteComponentModel.ContentLastUpdated) {
                switch (localComponentModel.Type) {
                    case "TextComponent":
                        localComponentModel.Content = remoteComponentModel.Content;
                        localComponentModel.ContentLastChanged = remoteComponentModel.ContentLastUpdated;
                        await localComponentModel.SaveAsync();
                        break;
                    case "ImageComponent":
                        var savingPath = localComponentModel.Content;
                        localComponentModel.ContentLastChanged = remoteComponentModel.LastUpdated;
                        await SynchronizationService.CloudApi.DownloadImage(remoteComponentModel.Content, savingPath);
                        await localComponentModel.SaveAsync();
                        break;
                }
            } else {
                switch (localComponentModel.Type) {
                    case "TextComponent":
                        remoteComponentModel.Content = localComponentModel.Content;
                        remoteComponentModel.ContentLastUpdated = localComponentModel.ContentLastChanged;
                        break;
                    case "ImageComponent":
                        if (localComponentModel.ContentLastChanged == remoteComponentModel.ContentLastUpdated) break;
                        
                        var imagePath = localComponentModel.Content;
                        var storageFile = await StorageFile.GetFileFromPathAsync(LocalFileHelper.ToAbsolutePath(imagePath));
                        var stream = await storageFile.OpenAsync(FileAccessMode.Read);

                        var imageId = await SynchronizationService.CloudApi.UploadImage(stream.AsStream(), remoteComponentModel.RemoteFileId);

                        if (imageId != null) {
                            remoteComponentModel.Content = imageId;
                            remoteComponentModel.ContentLastUpdated = Time.CurrentTimeMillis();
                        }
                        break;
                }
            }

            if (localComponentModel.DeletionLastChanged < remoteComponentModel.DeletionLastUpdated) {
                localComponentModel.Deleted = remoteComponentModel.Deleted;
                localComponentModel.DeletionLastChanged = remoteComponentModel.DeletionLastUpdated;
            } else {
                remoteComponentModel.Deleted = localComponentModel.Deleted;
                remoteComponentModel.DeletionLastUpdated = localComponentModel.DeletionLastChanged;
            }

            await localComponentModel.SaveAsync();

            return (localComponentModel, remoteComponentModel);
        }
        
        public static async Task<(Component, RemoteComponentModel)> CompareComponentModels(Component localComponent, RemoteComponentModel remoteComponentModel) {
            if (localComponent == null || remoteComponentModel == null) return (localComponent, remoteComponentModel);
            if (localComponent.GetContent().Equals(remoteComponentModel.Content) && localComponent.GetBounds().Equals(remoteComponentModel.Rectangle) && localComponent.GetZIndex().Equals(remoteComponentModel.ZIndex)) return (localComponent, remoteComponentModel);

            localComponent.CreateUserAction = false;
            
            if (localComponent.GetModel().PositionLastChanged < remoteComponentModel.PositionLastUpdated) {
                localComponent.SetBounds(new RectangleD(remoteComponentModel.Rectangle[0], remoteComponentModel.Rectangle[1], remoteComponentModel.Rectangle[2], remoteComponentModel.Rectangle[3]));
                localComponent.GetModel().PositionLastChanged = remoteComponentModel.PositionLastUpdated;
            } else {
                remoteComponentModel.Rectangle[0] = localComponent.GetModel().PosX;
                remoteComponentModel.Rectangle[1] = localComponent.GetModel().PosY;
                remoteComponentModel.PositionLastUpdated = localComponent.GetModel().PositionLastChanged;
            }

            if (localComponent.GetModel().SizeLastChanged < remoteComponentModel.SizeLastUpdated) {
                localComponent.SetBounds(new RectangleD(remoteComponentModel.Rectangle[0], remoteComponentModel.Rectangle[1], remoteComponentModel.Rectangle[2], remoteComponentModel.Rectangle[3]));
                localComponent.GetModel().SizeLastChanged = remoteComponentModel.SizeLastUpdated;
            } else {
                remoteComponentModel.Rectangle[2] = localComponent.GetModel().SizeX;
                remoteComponentModel.Rectangle[3] = localComponent.GetModel().SizeY;
                remoteComponentModel.SizeLastUpdated = localComponent.GetModel().SizeLastChanged;
            }

            if (localComponent.GetModel().ZIndexLastChanged < remoteComponentModel.ZIndexLastUpdated) {
                localComponent.SetZIndex(remoteComponentModel.ZIndex);
                localComponent.GetModel().ZIndexLastChanged = remoteComponentModel.ZIndexLastUpdated;
            } else {
                remoteComponentModel.ZIndex = localComponent.GetZIndex();
                remoteComponentModel.ZIndexLastUpdated = localComponent.GetModel().ZIndexLastChanged;
            }

            var contentLastChanged = localComponent.GetModel().ContentLastChanged;
            
            if (localComponent.GetModel().ContentLastChanged < remoteComponentModel.ContentLastUpdated) {
                switch (localComponent.GetModel().Type) {
                    case "TextComponent":
                        localComponent.SetContent(remoteComponentModel.Content);
                        localComponent.GetModel().ContentLastChanged = contentLastChanged;
                        await localComponent.GetModel().SaveAsync();
                        break;
                    case "ImageComponent":
                        var savingPath = localComponent.GetModel().Content;
                        localComponent.GetModel().ContentLastChanged = remoteComponentModel.LastUpdated;
                        await SynchronizationService.CloudApi.DownloadImage(remoteComponentModel.Content, savingPath);
                        await localComponent.GetModel().SaveAsync();

                        var imageComponent = (ImageComponent) localComponent;
                        
                        imageComponent.SetImageData(await LocalFileHelper.ReadBytes(savingPath));
                        
                        break;
                }
            } else {
                switch (localComponent.GetModel().Type) {
                    case "TextComponent":
                        remoteComponentModel.Content = localComponent.GetContent();
                        remoteComponentModel.ContentLastUpdated = localComponent.GetModel().ContentLastChanged;
                        break;
                    case "ImageComponent":
                        if (localComponent.GetModel().ContentLastChanged == remoteComponentModel.ContentLastUpdated) break;
                        
                        var imagePath = localComponent.GetContent();
                        var storageFile = await StorageFile.GetFileFromPathAsync(LocalFileHelper.ToAbsolutePath(imagePath));
                        var stream = await storageFile.OpenAsync(FileAccessMode.Read);

                        var imageId = await SynchronizationService.CloudApi.UploadImage(stream.AsStream(), remoteComponentModel.RemoteFileId);

                        if (imageId != null) {
                            remoteComponentModel.Content = imageId;
                            remoteComponentModel.ContentLastUpdated = Time.CurrentTimeMillis();
                        }
                        break;
                }
            }

            if (localComponent.GetModel().DeletionLastChanged < remoteComponentModel.DeletionLastUpdated) {
                localComponent.SetDeleted(remoteComponentModel.Deleted);
                localComponent.GetModel().DeletionLastChanged = remoteComponentModel.DeletionLastUpdated;
            } else {
                remoteComponentModel.Deleted = localComponent.GetDeleted();
                remoteComponentModel.DeletionLastUpdated = localComponent.GetModel().DeletionLastChanged;
            }

            await localComponent.GetModel().SaveAsync();

            localComponent.CreateUserAction = true;

            return (localComponent, remoteComponentModel);
        }
        
        public static void MergeComponentEditor(ComponentModel localComponentModel, RemoteComponentModel remoteComponentModel) {
            MainThread.BeginInvokeOnMainThread(async () => {
                if (localComponentModel == null) {
                    var cm = await CreateLocalComponentModel(remoteComponentModel);
                    var c = cm.ToComponent();
                
                    App.EditorScreen.AddContentComponent(c);
                
                    return;
                }
                
                if (remoteComponentModel == null) {
                    var remoteId = await CreateRemoteComponentModel(localComponentModel);

                    localComponentModel.LastSynced = Time.CurrentTimeMillis();

                    await FileHelper.SetLastSynced(localComponentModel);
                    var lc = App.EditorScreen.GetDocumentComponents().Find(component => component.ComponentId == localComponentModel.ComponentId);
                
                    if (lc != null) lc.SetRemoteId(remoteId);
                    
                    return;
                }
                
                var localComponent = App.EditorScreen.GetDocumentComponents().Find(component => component.ComponentId == localComponentModel.ComponentId);

                var (_, rcm) = await CompareComponentModels(localComponent, remoteComponentModel);
                await SynchronizationService.CloudApi.PutComponent(rcm);
            });
        }

        public static async Task MergeComponentDatabase(ComponentModel localComponentModel, RemoteComponentModel remoteComponentModel) {
            if (localComponentModel == null) {
                await CreateLocalComponentModel(remoteComponentModel);
                return;
            }

            if (remoteComponentModel == null) {
                var remoteId = await CreateRemoteComponentModel(localComponentModel);
                
                localComponentModel.LastSynced = Time.CurrentTimeMillis();
                localComponentModel.RemoteId = remoteId;

                await FileHelper.SetLastSynced(localComponentModel);
                await FileHelper.SetRemoteId(localComponentModel, remoteId);
                
                return;
            }
            
            if (localComponentModel.Type != remoteComponentModel.Type) return;
            var (lcm, rcm) = await CompareComponentModels(localComponentModel, remoteComponentModel);
            
            localComponentModel = lcm;
            remoteComponentModel = rcm;
            
            await FileHelper.UpdateComponentAsync(localComponentModel);
            await SynchronizationService.CloudApi.PutComponent(remoteComponentModel);
        }

        private static List<(ComponentModel, RemoteComponentModel)> MatchComponents(IEnumerable<ComponentModel> localComponents, IEnumerable<RemoteComponentModel> remoteComponentModels) {
            var result = new List<(ComponentModel, RemoteComponentModel)>();
            var remoteComponentModelsList = remoteComponentModels.ToList();
            foreach (var localComponentModel in localComponents.Where(model => model.ComponentId != -1)) {
                var matchingRemoteComponent = remoteComponentModelsList.FirstOrDefault(remoteComponentModel => remoteComponentModel.Id == localComponentModel.RemoteId);
                result.Add((localComponentModel, matchingRemoteComponent));
                if (matchingRemoteComponent != null) remoteComponentModelsList.Remove(matchingRemoteComponent);
            }

            result.AddRange(remoteComponentModelsList.Where(model => !string.IsNullOrEmpty(model.Id)).Select(remoteComponent => ((ComponentModel, RemoteComponentModel)) (null, remoteComponent)));
            return result;
        }
    }
}