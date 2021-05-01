using System;
using System.Collections.Generic;
using System.Linq;
using AINotes.Models;
using Helpers;
using Helpers.Essentials;
using Helpers.Extensions;
using MaterialComponents;
using Newtonsoft.Json;

namespace AINotes.Helpers {
    public static partial class LocalSharingHelper {
        private static void OnShareFileOfferReceived(RemoteDevice remoteDevice) {
            Logger.Log("[LocalSharingHelper]", "Receiver: OnShareFileOfferReceived");
            MainThread.BeginInvokeOnMainThread(() => {
                App.Page.Notifications.Add(new MDNotification("File Sharing Request", AcceptFile, DismissFile, false, true, ResourceHelper.GetString("accept"), ResourceHelper.GetString("dismiss"), $"Datei Transfer von {remoteDevice.Name}"));
            });

            async void AcceptFile(MDNotification hint) {
                Logger.Log("[LocalSharingHelper]", "Receiver: Accepted");

                // accept
                await SocketService.ServerSendStringToClient(remoteDevice, Messages.Accept);

                // receive
                hint.Update("Receiving...");
                var newFile = await SocketService.ServerReceiveStringFromClient(remoteDevice);
                await SocketService.ServerSendStringToClient(remoteDevice, Messages.Ok);
                SocketService.ServerDisconnectAllClients();

                // deserialize
                hint.Update("Deserializing...");
                var newFileModel = JsonConvert.DeserializeObject<FileModel>(newFile);
                var newFileComponents = JsonConvert.DeserializeObject<List<ComponentModel>>(newFileModel.ComponentModels);

                // create file
                hint.Update("Creating file...");
                var newFileId = await FileHelper.CreateFileAsync(newFileModel.Name, newFileModel.Subject, App.FileManagerScreen.CurrentDirectory.DirectoryId);
                await FileHelper.UpdateFileAsync(newFileId, lineMode: newFileModel.LineMode, strokeContent: newFileModel.StrokeContent);
                await FileHelper.SetShared(newFileId, true);

                // create components
                foreach (var componentModel in newFileComponents) {
                    componentModel.FileId = newFileId;
                    componentModel.ComponentId = default;

                    // save images for document components
                    if (componentModel.Type == "DocumentComponent") {
                        // var imgSavingPath =  await new ImageComponent().GetImageSavingPath();
                        // var imgBytes = componentModel.Content.Deserialize<byte[]>();
                        // await LocalFileHelper.WriteFileAsync(imgSavingPath, imgBytes);
                        // componentModel.Content = imgSavingPath;
                    }

                    await FileHelper.CreateComponentAsync(componentModel);
                }

                // update info & buttons
                void OpenFile(MDNotification _) {
                    MainThread.BeginInvokeOnMainThread(() => {
                        App.Page.Load(App.EditorScreen);
                        App.Page.Title = newFileModel.Name;
                        App.EditorScreen.LoadFile(newFileId);
                    });
                }

                hint.Update("File received successfully. Open it?", ResourceHelper.GetString("yes"), ResourceHelper.GetString("no"), true, true, OpenFile);
            }

            async void DismissFile(MDNotification hint) {
                Logger.Log("[LocalSharingHelper]", "Receiver: Dismissed");
                await SocketService.ServerSendStringToClient(remoteDevice, Messages.Dismiss);
                SocketService.ServerDisconnectAllClients();
            }
        }

        private static void OnLiveShareOfferReceived(RemoteDevice remoteDevice) {
            MainThread.BeginInvokeOnMainThread(() => {
                App.Page.Notifications.Add(new MDNotification("Live Share Offer", AcceptOffer, DismissOffer, false, true, ResourceHelper.GetString("accept"), ResourceHelper.GetString("dismiss"), $"Live Share Einladung von {remoteDevice.Name}"));
            });
            async void AcceptOffer(MDNotification hint) {
                Logger.Log("[LocalSharingHelper]", "Receiver: Accepted");
                
                // remove buttons
                hint.Update(hint.AdditionalInfo);

                // accept the connection
                await SocketService.ServerSendStringToClient(remoteDevice, Messages.Accept);

                // get connection token
                var liveShareToken = await SocketService.ServerReceiveStringFromClient(remoteDevice);
                liveShareToken = liveShareToken.Replace("ok|", "");

                SocketService.ServerDisconnectAllClients();
                await AcceptLiveShareOffer(remoteDevice, liveShareToken, hint.Update);
            }
            
            async void DismissOffer(MDNotification hint) {
                Logger.Log("[LocalSharingHelper]", "Receiver: Dismissed");
                await SocketService.ServerSendStringToClient(remoteDevice, Messages.Dismiss);
                SocketService.ServerDisconnectAllClients();
            }
        }

        private static async void OnLiveShareJoinReceived(RemoteDevice remoteDevice) {
            // check if live session is offered
            if (_currentLiveShareToken == null) {
                Logger.Log(" -> no offer");
                await SocketService.ServerSendStringToClient(remoteDevice, "no offer");
                SocketService.ServerDisconnectAllClients();
                return;
            }

            // check if client has the token for the session
            var clientToken = await SocketService.ServerReceiveStringFromClient(remoteDevice);
            if (_currentLiveShareToken != clientToken) {
                Logger.Log(" -> wrong offer/token");
                await SocketService.ServerSendStringToClient(remoteDevice, "wrong offer/token");
                SocketService.ServerDisconnectAllClients();
                return;
            }

            // accept client
            void Update(string msg, string acceptTxt = null, string dismissTxt = null, bool closeOnAccept = false, bool closeOnDismiss = false, Action<MDNotification> acceptAction = null, Action<MDNotification> dismissAction = null) {
                _currentLiveShareCallback?.Invoke(msg, acceptTxt, dismissTxt, closeOnAccept, closeOnDismiss, acceptAction, dismissAction);
            }

            await SocketService.ServerSendStringToClient(remoteDevice, Messages.Accept);
            Logger.Log("Client connect to live_share_join");
            Update("Client connection received");

            // send initial file
            IdDict = new Dictionary<int, int>();
            var fileJson = await FileHelper.GetFileJsonAsync(_currentLiveShareFileModel);

            var tempFileComponentModels = fileJson.Deserialize<FileModel>().ComponentModels.Deserialize<List<ComponentModel>>();
            foreach (var componentModel in tempFileComponentModels.Where(componentModel => componentModel.LiveId > _maxCurrentLiveId)) {
                IdDict.Add(componentModel.ComponentId, componentModel.LiveId);
                _maxCurrentLiveId = componentModel.LiveId;
            }

            await SocketService.ServerSendStringToClient(remoteDevice, fileJson);
            Logger.Log("Max Current Live ID:", _maxCurrentLiveId);

            // wait for client ok
            var clientOk = await SocketService.ServerReceiveStringFromClient(remoteDevice);
            if (clientOk != "ok") {
                Update($"Error: Client disconnected ({clientOk})", ResourceHelper.GetString("ok"), closeOnAccept: true);
                SocketService.ServerDisconnectAllClients();
                return;
            }

            Update("Client fully connected");

            // open the file
            MainThread.BeginInvokeOnMainThread(() => {
                if (App.Page.Content == App.EditorScreen) return;
                
                App.Page.Load(App.EditorScreen);
                App.Page.Title = _currentLiveShareFileModel.Name;
                App.EditorScreen.LoadFile(_currentLiveShareFileModel.FileId);
            });

            // update hint
            _disconnected = false;
            Update("Live!", "Disconnect", acceptAction: _ => {
                Update("Disconnecting...");
                LiveDisconnect();
                Update("Disconnected.", ResourceHelper.GetString("ok"), closeOnAccept: true);
            });

            // live update
            await LiveUpdater(
                () => SocketService.ServerReceiveStringFromClient(remoteDevice),
                s => SocketService.ServerSendStringToClient(remoteDevice, s), 
                SocketService.ServerDisconnectAllClients,
                remoteDevice
            );
        }
    }
}