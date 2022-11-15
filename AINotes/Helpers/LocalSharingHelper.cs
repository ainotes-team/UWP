using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Input.Inking;
using AINotes.Components;
using AINotes.Components.Implementations;
using AINotes.Models;
using Helpers;
using Helpers.Essentials;
using Helpers.Extensions;
using MaterialComponents;
using Newtonsoft.Json;
using Random = Helpers.Random;

namespace AINotes.Helpers {
    public class RemoteDevice {
        public string Id { get; }
        public string Name { get; }

        public RemoteDevice(string id, string name) {
            Id = id;
            Name = name;
        }

        public override string ToString() => $"{Name} ({Id})";
    }

    public struct Protocols {
        public const string ShareFileOffer = "share_file";
        public const string LiveShareOffer = "live_share_offer";
        public const string LiveShareJoin = "live_share_join";
        public const string Unknown = "unknown_protocol";
    }

    public struct Messages {
        public const string Accept = "accept";
        public const string Dismiss = "dismiss";
        public const string Ok = "ok";
        public const string Disconnect = "disconnect";
    }

    public enum LiveUpdate {
        ComponentAdded,
        ComponentUpdated,
        ComponentDeleted,
        
        StrokeAdded,
        StrokeChanged,
        StrokeRemoved,
    }
    
    // TODO: Multi-Share Compatibility
    public static partial class LocalSharingHelper {
        public static readonly SocketService SocketService;

        public static bool LiveSharing;

        static LocalSharingHelper() {
            SocketService = new SocketService();

            App.Current.Resuming += OnAppResuming;
        }
        
        public static void Initialize() {
            if (!Preferences.LocalSharingEnabled) return;
            
            Logger.Log("[LocalSharingHelper]", "-> Initialize");
            SocketService.StartBluetoothServer();
            SocketService.ServerRegisterConnectionReceivedListener(OnConnectionReceived);
            Logger.Log("[LocalSharingHelper]", "<- Initialize");
        }

        private static void OnAppResuming(object s, object e) {
            SocketService.RestartBluetoothServer();
        }

        private static async void OnConnectionReceived(RemoteDevice remoteDevice) {
            var message = await SocketService.ServerReceiveStringFromClient(remoteDevice);
            Logger.Log("[LocalSharingHelper]", "Received: " + message);
            switch (message) {
                case Protocols.ShareFileOffer:
                    OnShareFileOfferReceived(remoteDevice);
                    break;
                case Protocols.LiveShareOffer:
                    OnLiveShareOfferReceived(remoteDevice);
                    break;
                case Protocols.LiveShareJoin:
                    OnLiveShareJoinReceived(remoteDevice);
                    break;
                default:
                    await SocketService.ServerSendStringToClient(remoteDevice, Protocols.Unknown);
                    SocketService.ServerDisconnectAllClients();
                    break;
            }
        }

        public static async Task SendFile(RemoteDevice receiver, string fileString, Action<string, string, string, bool, bool, Action<MDNotification>, Action<MDNotification>> statusUpdateCallback = null) {
            void Update(string msg, string acceptTxt=null, string dismissTxt=null, bool closeOnAccept=false, bool closeOnDismiss=false) => statusUpdateCallback?.Invoke(msg, acceptTxt, dismissTxt, closeOnAccept, closeOnDismiss, null, null);
            
            Update(ResourceHelper.GetString("connect"));
            await SocketService.ClientConnectToServer(receiver);

            // request file share
            await SocketService.ClientSendString(Protocols.ShareFileOffer);
            Update(ResourceHelper.GetString("request_sent"));

            // get response
            var response = await SocketService.ClientReceiveString();
            if (response != Messages.Accept) {
                Update(ResourceHelper.GetString("request_dismissed"), ResourceHelper.GetString("ok"), closeOnAccept: true);
                return;
            }
            Update(ResourceHelper.GetString("request_accepted"));

            // send file
            Update(ResourceHelper.GetString("sending_file"));
            await SocketService.ClientSendString(fileString);

            // get response
            response = await SocketService.ClientReceiveString();
            if (response != Messages.Ok) {
                Logger.Log("[LocalSharingHelper]", "Client: send file error:", response);
                Update("Error" + response, ResourceHelper.GetString("ok"), closeOnAccept: true);
                return;
            }
            Update(ResourceHelper.GetString("file_sent_success"), ResourceHelper.GetString("ok"), closeOnAccept: true);
            
            // disconnect
            SocketService.ClientDisconnectServer();
        }

        private static string _currentLiveShareToken;
        private static FileModel _currentLiveShareFileModel;
        private static Action<string, string, string, bool, bool, Action<MDNotification>, Action<MDNotification>> _currentLiveShareCallback;
        public static async Task SendLiveShareOffer(List<RemoteDevice> receiverDevices, FileModel fileModel, Action<string, string, string, bool, bool, Action<MDNotification>, Action<MDNotification>> statusUpdateCallback = null) {
            void Update(string msg, string acceptTxt=null, string dismissTxt=null, bool closeOnAccept=false, bool closeOnDismiss=false, Action<MDNotification> acceptAction=null, Action<MDNotification> dismissAction=null) {
                statusUpdateCallback?.Invoke(msg, acceptTxt, dismissTxt, closeOnAccept, closeOnDismiss, acceptAction, dismissAction);
            }

            _maxCurrentLiveId = 0;
            _currentLiveShareFileModel = fileModel;
            _currentLiveShareCallback = statusUpdateCallback;
            _currentLiveShareToken = Random.String(20);

            foreach (var receiver in receiverDevices) {
                var devicePrefix = $"{receiverDevices.IndexOf(receiver)}/{receiverDevices.Count} ";
                
                Update(devicePrefix + ResourceHelper.GetString("connecting"));
                await SocketService.ClientConnectToServer(receiver);

                // request live share
                await SocketService.ClientSendString(Protocols.LiveShareOffer);
                Update(devicePrefix + ResourceHelper.GetString("request_sent"));

                // get response
                var response = await SocketService.ClientReceiveString();
                if (response != Messages.Accept) {
                    Update(devicePrefix + ResourceHelper.GetString("request_dismissed"), ResourceHelper.GetString("ok"), closeOnAccept: true);
                    return;
                }

                // send current token
                await SocketService.ClientSendString("ok|" + _currentLiveShareToken);
                Update(devicePrefix + "Waiting for remote to connect");
            
                // disconnect
                SocketService.ClientDisconnectServer();
            }
        }

        public static Dictionary<int, int> IdDict = new Dictionary<int, int>();
        public static async Task AcceptLiveShareOffer(RemoteDevice offerSender, string offerToken, Action<string, string, string, bool, bool, Action<MDNotification>, Action<MDNotification>> statusUpdateCallback = null) {
            void Update(string msg, string acceptTxt=null, string dismissTxt=null, bool closeOnAccept=false, bool closeOnDismiss=false, Action<MDNotification> acceptAction=null, Action<MDNotification> dismissAction=null) => statusUpdateCallback?.Invoke(msg, acceptTxt, dismissTxt, closeOnAccept, closeOnDismiss, acceptAction, dismissAction);

            _currentLiveShareCallback = statusUpdateCallback;

            IdDict = new Dictionary<int, int>();
            
            // connect
            Update(ResourceHelper.GetString("connecting"));
            await SocketService.ClientConnectToServer(offerSender);

            // request live share join
            await SocketService.ClientSendString(Protocols.LiveShareJoin);
            await SocketService.ClientSendString(offerToken);
            Update("Sharing Request sent");

            // get response
            var response = await SocketService.ClientReceiveString();
            if (response != Messages.Accept) {
                Update("Connection Error", "Ok", closeOnAccept: true);
                return;
            }
            
            Update("Connected");
            
            var initialFileModel = await SocketService.ClientReceiveString();
            Update("File received");
            
            // unpack
            var newFileModel = JsonConvert.DeserializeObject<FileModel>(initialFileModel);
            if (newFileModel == null) {
                Logger.Log("[LocalSharingHelper]", $"Receiver: Received Invalid FileModel", logLevel: LogLevel.Warning);
                Update("Error:\nDeserialisierung fehlgeschlagen.");
                return;
            }
            
            var newFileComponents = JsonConvert.DeserializeObject<List<ComponentModel>>(newFileModel.ComponentModels);
            if (newFileComponents == null) {
                Logger.Log("[LocalSharingHelper]", $"Receiver: Received Invalid FileComponents", logLevel: LogLevel.Warning);
                Update("Error:\nDeserialisierung fehlgeschlagen.");
                return;
            }
            
            // create file
            var newFileId = await FileHelper.CreateFileAsync(newFileModel.Name, newFileModel.Subject, App.FileManagerScreen.CurrentDirectory.DirectoryId);
            await FileHelper.UpdateFileAsync(newFileId, lineMode: newFileModel.LineMode, strokeContent: newFileModel.StrokeContent);
            await FileHelper.SetShared(newFileId, true);
            
            // create components
            foreach (var componentModel in newFileComponents) {
                componentModel.FileId = newFileId;
                componentModel.ComponentId = default;
                if (componentModel.LiveId > _maxCurrentLiveId) _maxCurrentLiveId = componentModel.LiveId;
                if (componentModel.Type == "DocumentComponent") {
                    // var imgSavingPath = await new ImageComponent().GetImageSavingPath();
                    // var imgBytes = componentModel.Content.Deserialize<byte[]>();
                    // await LocalFileHelper.WriteFileAsync(imgSavingPath, imgBytes);
                    // componentModel.Content = imgSavingPath;
                }
                var componentId = await FileHelper.CreateComponentAsync(componentModel);
                IdDict.Add(componentId, componentModel.LiveId);
            }
            Logger.Log("[LocalSharingHelper]", "Max Current Live ID:", _maxCurrentLiveId);

            // open the file
            if (App.Page.Content != App.EditorScreen) App.Page.Load(App.EditorScreen);
            App.Page.Title = newFileModel.Name;
            App.EditorScreen.LoadFile(newFileId);
            Update("Ready to go");
            
            // send "ok"
            await SocketService.ClientSendString(Messages.Ok);
            
            // update hint
            _disconnected = false;
            Update("Live!", "Disconnect", acceptAction: _ => {
                Update("Disconnecting...");
                LiveDisconnect();
                Update("Disconnected.", "Ok", closeOnAccept: true);
            });

            // live update
            await LiveUpdater(SocketService.ClientReceiveString, SocketService.ClientSendString, SocketService.ClientDisconnectServer);
        }
        
        private static bool _disconnected;
        private static int _maxCurrentLiveId;
        private static Dictionary<int, int> _strokeLocalToRemote = new Dictionary<int, int>();
        private static async Task LiveUpdater(Func<Task<string>> recvString, Func<string, Task> sendString, Action disconnect, RemoteDevice remoteDevice=null) {
            Logger.Log("[LocalSharingHelper]", $"-> LiveUpdater {IdDict.ToFString()}", logLevel: LogLevel.Verbose);
            LiveSharing = true;
            
            var lastComponentState = new Dictionary<int, string>();
            
            void SetStrokeIdOffset(IEnumerable<int> loadedStrokeIds) {
                var strokeIdList = loadedStrokeIds.ToList();
                Logger.Log("[LocalSharingHelper]", "SetStrokeIdOffset", strokeIdList.Count, logLevel: LogLevel.Warning);
                if (strokeIdList.Count == 0) return;
                var strokeIdOffset = strokeIdList.Min() - 1;
                Logger.Log("[LocalSharingHelper]", "SetStrokeIdOffset: StrokeIdOffset:", strokeIdOffset);
                Logger.Log("[LocalSharingHelper]", "SetStrokeIdOffset: A StrokeIds:", strokeIdList.ToFString(), logLevel: LogLevel.Debug);
                Logger.Log("[LocalSharingHelper]", "SetStrokeIdOffset: B StrokeIds:", strokeIdList.Select(id => id - strokeIdOffset).ToFString(), logLevel: LogLevel.Debug);

                // set initial dict
                _strokeLocalToRemote = strokeIdList.Select(id => id - strokeIdOffset).ToDictionary(id => id + strokeIdOffset);
                Logger.Log("[LocalSharingHelper]", "SetStrokeIdOffset: StrokeLocalToRemote:", _strokeLocalToRemote.ToFString(), logLevel: LogLevel.Debug);
            }

            App.EditorScreen.SubscribeInkChanges(SetStrokeIdOffset);
            await MainThread.InvokeOnMainThreadAsync(() => {
                SetStrokeIdOffset(App.EditorScreen.GetInkStrokes().Select(s => (int) s.Id));
            });
            
            async void OnEditorScreenOnComponentAdded(Component component) {
                Logger.Log("[LocalSharingHelper]", "-> OnEditorScreenOnComponentAdded", logLevel: LogLevel.Debug);
                try {
                    // do not sync if live id is not default
                    // if (component.LiveId != default) {
                    //     Logger.Log("[LocalSharingHelper]", "OnEditorScreenOnComponentAdded: component.LiveId != default -> returning", logLevel: LogLevel.Verbose);
                    //     return;
                    // }

                    // do not sync selection & shape components
                    if (component.GetType() == typeof(SelectionComponent)) return;
                    
                    // assign a new live id & update the current max live id
                    component.LiveId = _maxCurrentLiveId + 1;
                    _maxCurrentLiveId = component.LiveId;
                    
                    // send
                    Logger.Log("[LocalSharingHelper]", "OnEditorScreenOnComponentAdded:", component.LiveId, component.GetType(), $"(max: {_maxCurrentLiveId})");
                    await sendString(LiveUpdate.ComponentAdded + "|" + component.LiveId + "|" + component.GetType().Name + "|" + component.GetX() + "|" + component.GetY());
                } catch (Exception ex) {
                    Logger.Log("[LocalSharingHelper]", "Exception in OnEditorScreenOnComponentAdded:", ex.ToString(), logLevel: LogLevel.Error);
                } finally {
                    Logger.Log("[LocalSharingHelper]", "<- OnEditorScreenOnComponentAdded", logLevel: LogLevel.Debug);
                }
            }

            async void OnEditorScreenOnComponentChanged(Component component) {
                Logger.Log("[LocalSharingHelper]", "-> OnEditorScreenOnComponentChanged", logLevel: LogLevel.Debug);
                try {
                    // do not update selection components
                    if (component is SelectionComponent) return;
                    
                    // resolve live id <-> component id
                    if (component.LiveId == default) {
                        if (IdDict.ContainsKey(component.ComponentId)) {
                            component.LiveId = IdDict[component.ComponentId];
                            Logger.Log("[LocalSharingHelper]", $"OnEditorScreenOnComponentChanged: Resolved LiveId for {component.GetType().Name} {component.ComponentId} -> {component.LiveId}", logLevel: LogLevel.Verbose);
                        } else {
                            Logger.Log("[LocalSharingHelper]", $"OnEditorScreenOnComponentChanged: No LiveId for {component.GetType().Name} ({component.ComponentId})", logLevel: LogLevel.Warning);
                        }
                    }

                    // get the updated component model
                    var updatedModel = component.GetModel();

                    // do not sync null models
                    if (updatedModel == null) return;
                    
                    Logger.Log("[LocalSharingHelper]", "OnEditorScreenOnComponentChanged:", component.LiveId, updatedModel.Type, logLevel: LogLevel.Debug);
                    updatedModel.LiveId = component.LiveId;

                    // serialize document component data
                    if (component is ImageComponent /*documentComponent*/) {
                        // TODO: Fix
                        // updatedModel.Content = documentComponent.Data.Serialize();
                    }

                    // send
                    await sendString(LiveUpdate.ComponentUpdated + "|" + updatedModel.ToJson());
                } catch (Exception ex) {
                    Logger.Log("[LocalSharingHelper]", "Exception in OnEditorScreenOnComponentChanged:", ex.ToString(), logLevel: LogLevel.Error);
                } finally {
                    Logger.Log("[LocalSharingHelper]", "<- OnEditorScreenOnComponentChanged", logLevel: LogLevel.Debug);
                }
            }

            async void OnEditorScreenOnComponentDeleted(Component component) {
                Logger.Log("[LocalSharingHelper]", "-> OnEditorScreenOnComponentDeleted", logLevel: LogLevel.Debug);
                try {
                    // do not sync selection
                    if (component.GetType() == typeof(SelectionComponent)) return;

                    if (component.LiveId == default) {
                        component.LiveId = IdDict[component.ComponentId];
                    }
                    
                    // send the live id
                    Logger.Log("[LocalSharingHelper]", "OnEditorScreenOnComponentDeleted:", component.LiveId, component.GetType());
                    await sendString(LiveUpdate.ComponentDeleted + "|" + component.LiveId.ToString(CultureInfo.InvariantCulture));
                } catch (Exception ex) {
                    Logger.Log("[LocalSharingHelper]", "Exception in OnEditorScreenOnComponentDeleted:", ex.ToString(), logLevel: LogLevel.Error);
                } finally {
                    Logger.Log("[LocalSharingHelper]", "<- OnEditorScreenOnComponentDeleted", logLevel: LogLevel.Debug);
                }
            }
            
            async void OnEditorScreenOnStrokeAdded(InkStroke stroke) {
                Logger.Log("[LocalSharingHelper]", "-> OnEditorScreenOnStrokeAdded", logLevel: LogLevel.Verbose);
                try {
                    // add self to dict
                    try {
                        var localStrokeId = (int) stroke.Id;
                        _strokeLocalToRemote.Add(localStrokeId, localStrokeId);
                    } catch (Exception innerEx) {
                        Logger.Log("[LocalSharingHelper]", "innerException in OnEditorScreenOnStrokeAdded:", innerEx.ToString(), logLevel: LogLevel.Error);
                    }

                    // send
                    Logger.Log("[LocalSharingHelper]", $"OnEditorScreenOnStrokeAdded: Adding: {stroke.Id}", logLevel: LogLevel.Debug);
                    await sendString(LiveUpdate.StrokeAdded + "|" + stroke.Serialize());
                } catch (Exception ex) {
                    Logger.Log("[LocalSharingHelper]", "Exception in OnEditorScreenOnStrokeAdded:", ex.ToString(), logLevel: LogLevel.Error);
                } finally {
                    Logger.Log("[LocalSharingHelper]", "<- OnEditorScreenOnStrokeAdded", logLevel: LogLevel.Verbose);
                }
            }
            
            static void OnEditorScreenOnStrokeChanged(InkStroke stroke) {
                
            }
            
            async void OnEditorScreenOnStrokeRemoved(int inkStrokeId) {
                Logger.Log("[LocalSharingHelper]", "-> OnEditorScreenOnStrokeRemoved", logLevel: LogLevel.Verbose);
                try {
                    // resolved
                    var remoteStrokeId = _strokeLocalToRemote[inkStrokeId];
                    
                    // send the resolved strokeId
                    Logger.Log("[LocalSharingHelper]", $"OnEditorScreenOnStrokeRemoved: Removing: {inkStrokeId} -> {remoteStrokeId}", logLevel: LogLevel.Debug);
                    await sendString(LiveUpdate.StrokeRemoved + "|" + remoteStrokeId.ToString(CultureInfo.InvariantCulture));
                    
                    // remove stroke from dict
                    _strokeLocalToRemote.Remove(inkStrokeId);
                } catch (Exception ex) {
                    Logger.Log("[LocalSharingHelper]", "Exception in OnEditorScreenOnStrokeRemoved:", ex.ToString(), logLevel: LogLevel.Error);
                } finally {
                    Logger.Log("[LocalSharingHelper]", "<- OnEditorScreenOnStrokeRemoved", logLevel: LogLevel.Verbose);
                }
            }

            App.EditorScreen.ComponentAdded += OnEditorScreenOnComponentAdded;
            App.EditorScreen.ComponentChanged += OnEditorScreenOnComponentChanged;
            App.EditorScreen.ComponentDeleted += OnEditorScreenOnComponentDeleted;
            App.EditorScreen.StrokeAdded += OnEditorScreenOnStrokeAdded;
            App.EditorScreen.StrokeChanged += OnEditorScreenOnStrokeChanged;
            App.EditorScreen.StrokeRemoved += OnEditorScreenOnStrokeRemoved;

            while (true) {
                var updateString = await recvString();

                // => if server
                if (remoteDevice != null) {
                    await SocketService.ServerForwardMessage(remoteDevice, updateString);
                }
                
                if (updateString == null || updateString == Messages.Disconnect) break;
                var updateSplit = updateString.Split('|');
                var updateTypeString = updateSplit[0];
                var updateContentString = updateString.Substring(updateTypeString.Length+1);
                
                var success = Enum.TryParse(updateTypeString, out LiveUpdate updateType);
                if (!success) {
                    Logger.Log("[LocalSharingHelper]", "Unknown Update:", updateString, logLevel: LogLevel.Warning);
                    continue;
                }
                
                switch (updateType) {
                    case LiveUpdate.ComponentAdded:
                        Logger.Log("[LocalSharingHelper]", "-> ComponentAdded", updateContentString.Truncate(50, "..."));
                        var updateContentSplit = updateContentString.Split('|');
                        MainThread.BeginInvokeOnMainThread(() => {
                            var newComponent = new ComponentModel {
                                Type = updateContentSplit[1],
                            }.ToComponent();
                            if (newComponent.GetType() == typeof(TextComponent)) newComponent.SetContent("-");
                            newComponent.LiveId = int.Parse(updateContentSplit[0]);
                            App.EditorScreen.AddDocumentComponent(newComponent, new RectangleD(double.Parse(updateContentSplit[2]), double.Parse(updateContentSplit[3]), 100, 100));
                            newComponent.IsSelected = false;
                            if (newComponent.LiveId > _maxCurrentLiveId) _maxCurrentLiveId = newComponent.LiveId;
                            Logger.Log("[LocalSharingHelper]", "<- ComponentAdded", newComponent.ComponentId, "|", newComponent.LiveId);
                        });
                        break;
                    case LiveUpdate.ComponentUpdated:
                        Logger.Log("[LocalSharingHelper]", "ComponentUpdated", updateContentString.Truncate(50, "..."));
                        var updatedComponentModel = JsonConvert.DeserializeObject<ComponentModel>(updateContentString);
                        MainThread.BeginInvokeOnMainThread(() => {
                            if (updatedComponentModel == null) return;
                            var updatedComponent = App.EditorScreen.GetDocumentComponents().FirstOrDefault(v => v.LiveId == updatedComponentModel.LiveId);
                            if (updatedComponent == null) return;
                            if (updatedComponentModel.Content == null || (updatedComponent is ImageComponent && updatedComponentModel.Content == "null")) return;
                            
                            Logger.Log("[LocalSharingHelper]", $"updatedComponent.SetData({updatedComponentModel.Content.Truncate(100, "...")})");
                            
                            // set the updated component data if changed
                            if (updatedComponentModel.Content != "unchanged") {
                                updatedComponent.SetContent(updatedComponentModel.Content);
                                
                                // update last sync state
                                if (lastComponentState.ContainsKey(updatedComponentModel.LiveId)) {
                                    lastComponentState[updatedComponentModel.LiveId] = updatedComponentModel.Content;
                                } else {
                                    lastComponentState.Add(updatedComponentModel.LiveId, updatedComponentModel.Content);
                                }
                            }
                            
                            // set the updated component bounds
                            updatedComponent.SetBounds(new RectangleD(updatedComponentModel.Position.Item1, updatedComponentModel.Position.Item2, updatedComponentModel.Size.Item1, updatedComponentModel.Size.Item2));
                        });
                        break;
                    case LiveUpdate.ComponentDeleted:
                        Logger.Log("[LocalSharingHelper]", "ComponentDeleted", updateContentString.Truncate(50, "..."));
                        var removedComponentLiveId = int.Parse(updateContentString, CultureInfo.InvariantCulture);
                        var component = App.EditorScreen.GetDocumentComponents().FirstOrDefault(v => v.LiveId == removedComponentLiveId);
                        if (component == null) return;
                        MainThread.BeginInvokeOnMainThread(() => component.Delete());
                        break;
                    case LiveUpdate.StrokeAdded:
                        Logger.Log("[LocalSharingHelper]", "-> LiveUpdate.StrokeAdded", logLevel: LogLevel.Verbose);
                        try {
                            // parse remotely added stroke
                            var addedStroke = updateContentString.Deserialize<InkStroke>();
                            var addedStrokeRemoteId = (int) addedStroke.Id;
                            
                            // add the stroke transporter
                            await MainThread.InvokeOnMainThreadAsync(() => {
                                var addedStrokeLocalId = App.EditorScreen.AddInkStroke(addedStroke);
                                Logger.Log("[LocalSharingHelper]", $"LiveUpdate.StrokeAdded: Adding: {addedStrokeRemoteId} -> {addedStrokeLocalId}", logLevel: LogLevel.Debug);
                                
                                // add to dict
                                _strokeLocalToRemote.Add(addedStrokeLocalId, addedStrokeRemoteId);
                            });
                        } catch (Exception ex) {
                            Logger.Log("[LocalSharingHelper]", "Exception in LiveUpdate.StrokeAdded", ex.ToString(), logLevel: LogLevel.Error);
                        } finally {
                            Logger.Log("[LocalSharingHelper]", "<- LiveUpdate.StrokeAdded", logLevel: LogLevel.Verbose);
                        }
                        break;
                    case LiveUpdate.StrokeChanged:
                        Logger.Log("[LocalSharingHelper]", "-> LiveUpdate.StrokeChanged", logLevel: LogLevel.Verbose);
                        try {
                            // parse remotely changed stroke
                            var changedInkStroke = updateContentString.Deserialize<InkStroke>();
                            var changedStrokeRemoteId = (int) changedInkStroke.Id;
                            
                            // translate the remote stroke id to a local id
                            var changedStrokeLocalId = _strokeLocalToRemote.ReverseLookup(changedStrokeRemoteId);

                            // change the translated stroke id
                            Logger.Log("[LocalSharingHelper]", $"LiveUpdate.StrokeChanged: Changing: {changedStrokeRemoteId} -> {changedStrokeLocalId}", logLevel: LogLevel.Debug);
                            try {
                                // read stroke
                                await MainThread.InvokeOnMainThreadAsync(() => App.EditorScreen.DeleteInkStrokeById(changedStrokeLocalId));
                                var newLocalStrokeId = -1;
                                await MainThread.InvokeOnMainThreadAsync(() => newLocalStrokeId = App.EditorScreen.AddInkStroke(changedInkStroke));
                                
                                // update reference
                                _strokeLocalToRemote.Remove(changedStrokeLocalId);
                                _strokeLocalToRemote.Add(newLocalStrokeId, changedStrokeRemoteId);
                            } catch (KeyNotFoundException keyNotFoundException) {
                                Logger.Log("[LocalSharingHelper]", "LiveUpdate.StrokeChanged: KeyNotFoundException:", keyNotFoundException.ToString(), logLevel: LogLevel.Error);
                            }
                        } catch (Exception ex) {
                            Logger.Log("[LocalSharingHelper]", "Exception in LiveUpdate.StrokeChanged", ex.ToString(), logLevel: LogLevel.Error);
                        } finally {
                            Logger.Log("[LocalSharingHelper]", "<- LiveUpdate.StrokeChanged", logLevel: LogLevel.Verbose);
                        }
                        break;
                    case LiveUpdate.StrokeRemoved:
                        Logger.Log("[LocalSharingHelper]", "-> LiveUpdate.StrokeRemoved", logLevel: LogLevel.Verbose);
                        try {
                            // parse remotely removed stroke id
                            var removedStrokeRemoteId = int.Parse(updateContentString, CultureInfo.InvariantCulture);
                            
                            // translate the remote stroke id to a local id
                            var removedStrokeLocalId = _strokeLocalToRemote.ReverseLookup(removedStrokeRemoteId);

                            // remove the translated stroke id
                            Logger.Log("[LocalSharingHelper]", $"LiveUpdate.StrokeRemoved: Removing: {removedStrokeRemoteId} -> {removedStrokeLocalId}", logLevel: LogLevel.Debug);
                            try {
                                await MainThread.InvokeOnMainThreadAsync(() => App.EditorScreen.DeleteInkStrokeById(removedStrokeLocalId));
                            } catch (KeyNotFoundException keyNotFoundException) {
                                Logger.Log("[LocalSharingHelper]", "LiveUpdate.StrokeRemoved: KeyNotFoundException:", keyNotFoundException.ToString(), logLevel: LogLevel.Error);
                            }
                            
                            // remove from dict
                            _strokeLocalToRemote.Remove(removedStrokeLocalId);

                        } catch (Exception ex) {
                            Logger.Log("[LocalSharingHelper]", "Exception in LiveUpdate.StrokeRemoved", ex.ToString(), logLevel: LogLevel.Error);
                        } finally {
                            Logger.Log("[LocalSharingHelper]", "<- LiveUpdate.StrokeRemoved", logLevel: LogLevel.Verbose);
                        }
                        break;
                    default:
                        Logger.Log("[LocalSharingHelper]", "Unknown Update:", updateString, logLevel: LogLevel.Warning);
                        break;
                }
            }
            
            // client disconnected
            if (!_disconnected) {
                MainThread.BeginInvokeOnMainThread(() => {
                    Logger.Log("[LocalSharingHelper]", "Remote disconnected");
                    App.Page.GoBack();
                    _currentLiveShareCallback?.Invoke("Remote disconnected", "Ok", null, true, true, null, null);
                    disconnect();
                    _disconnected = true;
                });
            }

            App.EditorScreen.ComponentAdded -= OnEditorScreenOnComponentAdded;
            App.EditorScreen.ComponentChanged -= OnEditorScreenOnComponentChanged;
            App.EditorScreen.ComponentDeleted -= OnEditorScreenOnComponentDeleted;
            App.EditorScreen.StrokeAdded -= OnEditorScreenOnStrokeAdded;
            App.EditorScreen.StrokeChanged -= OnEditorScreenOnStrokeChanged;
            App.EditorScreen.StrokeRemoved -= OnEditorScreenOnStrokeRemoved;
            App.EditorScreen.UnsubscribeInkChanges(SetStrokeIdOffset);

            LiveSharing = false;
        }

        public static async void LiveDisconnect() {
            if (_disconnected) return;
            await SocketService.ClientSendString(Messages.Disconnect);
            SocketService.ServerDisconnectAllClients();
            App.Page.GoBack();
            _disconnected = true;
        }
    }
}