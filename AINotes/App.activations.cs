using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using AINotes.Components.Implementations;
using AINotes.Controls.Popups;
using AINotes.Helpers;
using Helpers;
using Helpers.Extensions;
using AINotes.Models;
using AINotesCloud;
using MaterialComponents;
using Newtonsoft.Json;

namespace AINotes {
    public partial class App {
        /// Default Activation
        /// Handles:
        ///     - Opening the App from the Systray
        ///     - Opening the App from another App
        ///     - Opening the App from a URI
        protected override async void OnActivated(IActivatedEventArgs args) {
            Logger.Log("[App]", "OnActivated", args.Kind, args.PreviousExecutionState);
            if (args.PreviousExecutionState != ApplicationExecutionState.Running) {
                Activate();
            }

            switch (args.Kind) {
                // ReSharper disable once InvertIf
                case ActivationKind.Protocol: {
                    // ReSharper disable once InvertIf
                    if (args is ProtocolActivatedEventArgs eventArgs) {
                        var uriParts = eventArgs.Uri.AbsoluteUri.Split(":");
                        var uriProtocol = uriParts[0];
                        switch (uriProtocol) {
                            case "ainotes": {
                                var location = uriParts[1];
                                var fileId = uriParts[2];

                                switch (location) {
                                    case "local": {
                                        try {
                                            // open the file
                                            var success = int.TryParse(fileId, out var localFileId);
                                            if (success) {
                                                try {
                                                    await FileHelper.GetFileAsync(localFileId);
                                                } catch (Exception) {
                                                    success = false;
                                                }
                                            }

                                            if (!success) {
                                                new MDContentPopup("Fehler", new MDLabel("Die angegeben URI konnte nicht gefunden werden.")).Show();
                                            } else {
                                                switch (args.PreviousExecutionState) {
                                                    case ApplicationExecutionState.Running: {
                                                        if (Page.Content != EditorScreen) Page.Load(EditorScreen);
                                                        EditorScreen.LoadFile(localFileId);
                                                        break;
                                                    }
                                                    case ApplicationExecutionState.NotRunning: {
                                                        void LoadFile(object _, object __) {
                                                            FileManagerScreen.Loaded -= LoadFile;
                                                            if (Page.Content != EditorScreen) Page.Load(EditorScreen);
                                                            EditorScreen.LoadFile(localFileId);
                                                        }

                                                        FileManagerScreen.Loaded += LoadFile;
                                                        break;
                                                    }
                                                }
                                            }
                                        } catch (Exception ex) {
                                            Logger.Log("[App]", "OnActivated ainotes:local protocol exception:", ex, logLevel: LogLevel.Error);
                                        }

                                        break;
                                    }
                                    case "remote": {
                                        try {
                                            if (!string.IsNullOrWhiteSpace(fileId)) {
                                                // check if file with remote id exists locally
                                                var localItem = FileHelper.ListFiles(-1).FirstOrDefault(f => f.RemoteId == fileId);
                                                if (localItem != null) {
                                                    switch (args.PreviousExecutionState) {
                                                        case ApplicationExecutionState.Running: {
                                                            if (Page.Content != EditorScreen) Page.Load(EditorScreen);
                                                            EditorScreen.LoadFile(localItem.FileId);
                                                            break;
                                                        }
                                                        case ApplicationExecutionState.NotRunning: {
                                                            void LoadFile(object _, object __) {
                                                                FileManagerScreen.Loaded -= LoadFile;
                                                                if (Page.Content != EditorScreen) Page.Load(EditorScreen);
                                                                EditorScreen.LoadFile(localItem.FileId);
                                                            }

                                                            FileManagerScreen.Loaded += LoadFile;
                                                            break;
                                                        }
                                                    }
                                                } else {
                                                    // otherwise download the file with the given remote id
                                                    Logger.Log("[App]", "Opening Remote File - Downloading");
                                                    if (SynchronizationService.CloudApi == null) {
                                                        Logger.Log("[App]", "Opening Remote File - Error: SynchronizationService.CloudApi == null", logLevel: LogLevel.Warning);
                                                        new MDContentPopup("Fehler", new MDLabel("SynchronizationService.CloudApi == null")).Show();
                                                        return;
                                                    }
                                                
                                                    // convert to FileModel
                                                    // var newToLocal = (await SynchronizationService.CloudApi.GetFile(fileId)).ToLocal();
                                                    // Logger.Log("newToLocal", newToLocal);
                                                    // newToLocal.LastSynced = newToLocal.LastChangedDate;
                                                    //
                                                    // // create local file
                                                    // var newLocalFileId = await FileHelper.CreateFileAsync(newToLocal);
                                                    // await FileHelper.SetLastSynced(newToLocal);
                                                    // Logger.Log("newLocalFileId", newLocalFileId);
                                                    //
                                                    // // create components
                                                    // var remoteComponents = await SynchronizationService.CloudApi.GetRemoteComponents(newToLocal.RemoteId);
                                                    //
                                                    // Logger.Log("create", remoteComponents.Count, "components");
                                                    // foreach (var remoteComponent in remoteComponents) {
                                                    //     var toLocal = await remoteComponent.ToLocalAsync(newLocalFileId);
                                                    //
                                                    //     var toLocalPlId = await FileHelper.CreateComponentAsync(toLocal);
                                                    //     toLocal.ComponentId = toLocalPlId;
                                                    //
                                                    //     // get remote image if documentComponent
                                                    //     // ReSharper disable once InvertIf
                                                    //     if (toLocal.Type == "DocumentComponent") {
                                                    //         var contentPath = ImageComponent.GetImageSavingPathStatic(toLocalPlId);
                                                    //         await SynchronizationService.CloudApi.DownloadImage(remoteComponent.Content, contentPath);
                                                    //         toLocal.Content = contentPath;
                                                    //
                                                    //         await FileHelper.UpdateComponentAsync(toLocal);
                                                    //     }
                                                    // }
                                                    //
                                                    // Logger.Log("open");
                                                    // switch (args.PreviousExecutionState) {
                                                    //     case ApplicationExecutionState.Running: {
                                                    //         if (Page.Content != EditorScreen) Page.Load(EditorScreen);
                                                    //         EditorScreen.LoadFile(newLocalFileId);
                                                    //         break;
                                                    //     }
                                                    //     case ApplicationExecutionState.NotRunning: {
                                                    //         void LoadFile(object _, object __) {
                                                    //             FileManagerScreen.Loaded -= LoadFile;
                                                    //             if (Page.Content != EditorScreen) Page.Load(EditorScreen);
                                                    //             EditorScreen.LoadFile(newLocalFileId);
                                                    //         }
                                                    //
                                                    //         
                                                    //         FileManagerScreen.Loaded += LoadFile;
                                                    //         break;
                                                    //     }
                                                    // }
                                                }
                                            } else {
                                                new MDContentPopup("Fehler", new MDLabel("Die angegeben URI konnte nicht gefunden werden.")).Show();
                                            }
                                        } catch (Exception ex) {
                                            new MDContentPopup("Fehler", new MDLabel("Die angegeben URI konnte nicht geöffnet werden.")).Show();
                                            Logger.Log("[App]", "OnActivated ainotes:remote protocol exception:", ex, logLevel: LogLevel.Error);
                                        }

                                        break;
                                    }
                                }

                                break;
                            }
                        }
                    }

                    break;
                }
            }

            base.OnActivated(args);
        }

        /// File Activation
        /// Handles:
        ///     - Opening the App through a File
        protected override async void OnFileActivated(FileActivatedEventArgs args) {
            Logger.Log("[App]", "OnFileActivated", args.Kind, args.PreviousExecutionState, args.Files);
            if (args.PreviousExecutionState != ApplicationExecutionState.Running) {
                Activate();
            }

            foreach (var itm in args.Files) {
                if (itm is not StorageFile f) continue;
                try {
                    // read file
                    var fileJson = await FileIO.ReadTextAsync(f);

                    // deserialize
                    var newFileModel = JsonConvert.DeserializeObject<FileModel>(fileJson);
                    var newFileComponents = JsonConvert.DeserializeObject<List<ComponentModel>>(newFileModel.ComponentModels);

                    // create file
                    var newFileId = await FileHelper.CreateFileAsync(newFileModel.Name, newFileModel.Subject, FileManagerScreen.CurrentDirectory.DirectoryId);
                    await FileHelper.UpdateFileAsync(newFileId, lineMode: newFileModel.LineMode, strokeContent: newFileModel.StrokeContent);

                    // create components
                    foreach (var componentModel in newFileComponents) {
                        componentModel.FileId = newFileId;
                        componentModel.ComponentId = default;
                        if (componentModel.Type == "DocumentComponent") {
                            var imgSavingPath = new ImageComponent(null).GetImageSavingPath();
                            var imgBytes = componentModel.Content.Deserialize<byte[]>();
                            await LocalFileHelper.WriteFileAsync(imgSavingPath, imgBytes);
                            componentModel.Content = imgSavingPath;
                        }

                        await FileHelper.CreateComponentAsync(componentModel);
                    }

                    // open the file
                    if (Page.Content != EditorScreen) Page.Load(EditorScreen);
                    Page.Title = newFileModel.Name;
                    EditorScreen.LoadFile(newFileId);
                } catch (Exception ex) {
                    Page.Notifications.Add(new MDNotification($"Import failed:\n{ex}"));
                    Logger.Log("[App]", "OnFileActivated: Failed to import file: ", ex, logLevel: LogLevel.Error);
                }
            }

            base.OnFileActivated(args);
        }

        /// Background Activation
        /// Handles:
        ///     - Receiving Connections from the FullTrustComponent
        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args) {
            Logger.Log("[App]", "OnBackgroundActivated");
            base.OnBackgroundActivated(args);

            if (args.TaskInstance.TriggerDetails is AppServiceTriggerDetails details) {
                HandleAppServiceBackgroundActivation(args, details);
            }
        }

        /// Share Activation
        /// Handles: Share Target
        ///     https://github.com/microsoft/Windows-universal-samples/blob/master/Samples/ShareTarget/cs/ShareTarget.xaml.cs
        ///     https://docs.microsoft.com/en-us/windows/uwp/app-to-app/share-data
        ///     https://docs.microsoft.com/en-us/windows/uwp/app-to-app/receive-data
        protected override async void OnShareTargetActivated(ShareTargetActivatedEventArgs args) {
            Logger.Log("[App]", "OnShareTargetActivated");
            base.OnShareTargetActivated(args);
            var shareOperation = args.ShareOperation;

            await Task.Factory.StartNew(async () => {
                var dataTitle = shareOperation.Data.Properties.Title;
                var dataDescription = shareOperation.Data.Properties.Description;
                var dataPackageFamilyName = shareOperation.Data.Properties.PackageFamilyName;
                var dataContentSourceWebLink = shareOperation.Data.Properties.ContentSourceWebLink;
                var dataContentSourceApplicationLink = shareOperation.Data.Properties.ContentSourceApplicationLink;
                var dataLogoBackgroundColor = shareOperation.Data.Properties.LogoBackgroundColor;
                var dataSquare30X30Logo = shareOperation.Data.Properties.Square30x30Logo;
                var sharedThumbnailStreamRef = shareOperation.Data.Properties.Thumbnail;
                var shareQuickLinkId = shareOperation.QuickLinkId;
                Logger.Log("OnShareTargetActivated:", dataTitle, dataDescription, dataPackageFamilyName, dataContentSourceWebLink, dataContentSourceApplicationLink, dataLogoBackgroundColor, dataSquare30X30Logo, sharedThumbnailStreamRef, shareQuickLinkId);
                Logger.Log("OnShareTargetActivated: Contacts:", shareOperation.Contacts.ToFString());
                Logger.Log("OnShareTargetActivated: AvailableFormats:", shareOperation.Data.AvailableFormats.ToFString());
                Logger.Log("OnShareTargetActivated: RequestedOperation:", shareOperation.Data.RequestedOperation);
                Logger.Log("OnShareTargetActivated: RequestedOperation:", shareOperation.Data.Properties.Keys.ToFString());
                if (shareOperation.Data.Contains(StandardDataFormats.WebLink)) {
                    try {
                        var sharedWebLink = await shareOperation.Data.GetWebLinkAsync();
                        Logger.Log("sharedWebLink", sharedWebLink);
                    } catch (Exception ex) {
                        Logger.Log("Failed GetWebLinkAsync - " + ex.Message);
                    }
                }

                if (shareOperation.Data.Contains(StandardDataFormats.ApplicationLink)) {
                    try {
                        var sharedApplicationLink = await shareOperation.Data.GetApplicationLinkAsync();
                        Logger.Log("sharedApplicationLink", sharedApplicationLink);
                    } catch (Exception ex) {
                        Logger.Log("Failed GetApplicationLinkAsync - " + ex.Message);
                    }
                }

                if (shareOperation.Data.Contains(StandardDataFormats.Text)) {
                    try {
                        var sharedText = await shareOperation.Data.GetTextAsync();
                        Logger.Log("sharedText", sharedText);
                    } catch (Exception ex) {
                        Logger.Log("Failed GetTextAsync - " + ex.Message);
                    }
                }

                if (shareOperation.Data.Contains(StandardDataFormats.StorageItems)) {
                    try {
                        var sharedStorageItems = await shareOperation.Data.GetStorageItemsAsync();
                        Logger.Log("sharedStorageItems", sharedStorageItems);
                    } catch (Exception ex) {
                        Logger.Log("Failed GetStorageItemsAsync - " + ex.Message);
                    }
                }

                if (shareOperation.Data.Contains("http://schema.org/Book")) {
                    try {
                        var sharedCustomData = await shareOperation.Data.GetTextAsync("http://schema.org/Book");
                        Logger.Log("sharedCustomData", sharedCustomData);
                    } catch (Exception ex) {
                        Logger.Log("Failed GetTextAsync(http://schema.org/Book)- " + ex.Message);
                    }
                }

                if (shareOperation.Data.Contains(StandardDataFormats.Html)) {
                    try {
                        var sharedHtmlFormat = await shareOperation.Data.GetHtmlFormatAsync();
                        Logger.Log("sharedHtmlFormat", sharedHtmlFormat);
                    } catch (Exception ex) {
                        Logger.Log("Failed GetHtmlFormatAsync - " + ex.Message);
                    }

                    try {
                        var sharedResourceMap = await shareOperation.Data.GetResourceMapAsync();
                        Logger.Log("sharedResourceMap", sharedResourceMap);
                    } catch (Exception ex) {
                        Logger.Log("Failed GetResourceMapAsync - " + ex.Message);
                    }
                }

                if (shareOperation.Data.Contains(StandardDataFormats.Bitmap)) {
                    try {
                        var sharedBitmapStreamRef = await shareOperation.Data.GetBitmapAsync();
                        Logger.Log("sharedBitmapStreamRef", sharedBitmapStreamRef);
                    } catch (Exception ex) {
                        Logger.Log("Failed GetBitmapAsync - " + ex.Message);
                    }
                }

                shareOperation.ReportCompleted();
            });
        }
    }
}