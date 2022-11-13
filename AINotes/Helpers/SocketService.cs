using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Helpers;
using Helpers.Extensions;

namespace AINotes.Helpers {
    public class ServerClientConnection {
        public readonly BluetoothDevice RemoteDevice;
        public readonly StreamSocket Socket;
        public readonly DataWriter Writer;
        public readonly DataReader Reader;

        public string Name => RemoteDevice.Name;

        public ServerClientConnection(BluetoothDevice remoteDevice, StreamSocket socket, DataWriter writer, DataReader reader) {
            RemoteDevice = remoteDevice;
            Socket = socket;
            Writer = writer;
            Reader = reader;
        }
    }
    
    public class SocketService {
        private StreamSocketListener _btSocketListener;
        private readonly BackgroundWorker _backgroundWorker;
        
        public SocketService() {
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.DoWork += OnBackgroundWorkerDoWork;
            _backgroundWorker.ProgressChanged += OnBackgroundWorkerProgressChanged;
            _backgroundWorker.RunWorkerCompleted += OnBackgroundWorkerCompleted;
            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.WorkerSupportsCancellation = true;
        }
        
        private async void OnBackgroundWorkerDoWork(object sender, DoWorkEventArgs args) {
            Logger.Log("[SocketService]", "BTServer:", "DoWork", sender);
            Logger.Log("[SocketService]", "BTServer:", "Starting...");
            try {
                // provider
                var rfcommProvider = await RfcommServiceProvider.CreateAsync(RfcommServiceId.FromUuid(Configuration.RfcommChatServiceUuid));

                // listener
                _btSocketListener = new StreamSocketListener();
                _btSocketListener.ConnectionReceived += OnBtConnectionReceived;

                // bind
                await _btSocketListener.BindServiceNameAsync(rfcommProvider.ServiceId.AsString(), SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);

                // init service sdp
                using (var sdpWriter = new DataWriter()) {
                    sdpWriter.WriteByte(Configuration.SdpServiceNameAttributeType);
                    sdpWriter.WriteByte((byte) Configuration.SdpServiceName.Length);
                    sdpWriter.UnicodeEncoding = UnicodeEncoding.Utf8;
                    sdpWriter.WriteString(Configuration.SdpServiceName);

                    var sdpBuffer = sdpWriter.DetachBuffer();
                    rfcommProvider.SdpRawAttributes.Add(Configuration.SdpServiceNameAttributeId, sdpBuffer);
                }

                // start advertising
                try {
                    rfcommProvider.StartAdvertising(_btSocketListener, true);
                } catch (Exception ex) {
                    Logger.Log("[SocketService]", "BTServer: StartAdvertising caused ", ex.ToString(), logLevel: LogLevel.Error);
                }

                if (Thread.CurrentThread.Name == null) {
                    Thread.CurrentThread.Name = "SocketService - BackgroundWorker";
                    Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                    Logger.Log("[SocketService]", "BTServer Thread:", Thread.CurrentThread.ManagedThreadId, "@", Thread.CurrentThread.Priority, logLevel: LogLevel.Warning);
                }

                Logger.Log("[SocketService]", "BTServer:", "Listening...");
                while (!((BackgroundWorker) sender).CancellationPending) {
                    Thread.Sleep(10000);
                }

                Logger.Log("[SocketService]", "BTServer:", "Cancelled.", logLevel: LogLevel.Warning);
            } catch (Exception ex) {
                Logger.Log("[SocketService]", "BTServer:", ex.ToString(), logLevel: LogLevel.Error);
            } finally {
                Logger.Log("[SocketService]", "BTServer: Finally", logLevel: LogLevel.Warning);
            }
        }

        private void OnBackgroundWorkerProgressChanged(object sender, ProgressChangedEventArgs args) {
            Logger.Log("[SocketService]", "BackgroundWorker: ProgressChanged", args.ProgressPercentage, logLevel: LogLevel.Warning);
        }

        private void OnBackgroundWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Cancelled) {
                Logger.Log("[SocketService]", "BackgroundWorker: Process was cancelled", logLevel: LogLevel.Warning);
            } else if (e.Error != null) {
                Logger.Log("[SocketService]", "BackgroundWorker: There was an error running the process. The thread aborted", e.Error.ToString(), logLevel: LogLevel.Error);
            } else {
                Logger.Log("[SocketService]", "BackgroundWorker: Process was completed", e.Result, logLevel: LogLevel.Verbose);
            }
        }

        public void StartBluetoothServer() {
            Logger.Log("[SocketService]", "-> StartBluetoothServer");
            if (!_backgroundWorker.CancellationPending) _backgroundWorker.CancelAsync();
            _backgroundWorker.RunWorkerAsync();
            
            Logger.Log("[SocketService]", "<- StartBluetoothServer");
        }

        public void StopBluetoothServer() {
            if (_btSocketListener == null) return;
            Logger.Log("[SocketService]", "BTServer:", "Stopping...");
            
            _btSocketListener.Dispose();
            _btSocketListener = null;

            
            Logger.Log("[SocketService]", "BTServer:", "Stopped");
        }

        public void RestartBluetoothServer() {
            Logger.Log("[SocketService]", "BTServer:", "Restarting...");
            StopBluetoothServer();
            // TODO: check if _backgroundWorker.IsBusy
            StartBluetoothServer();
        }

        private Action<RemoteDevice> _connectionReceivedCallback;
        public void ServerRegisterConnectionReceivedListener(Action<RemoteDevice> callback) => _connectionReceivedCallback = callback;

        private readonly Dictionary<RemoteDevice, ServerClientConnection> _serverClientConnectionsDict = new Dictionary<RemoteDevice, ServerClientConnection>();
        private Dictionary<RemoteDevice, ServerClientConnection>.ValueCollection ServerClientConnections => _serverClientConnectionsDict.Values;
        

        public void ServerDisconnectAllClients() {
            Logger.Log("[SocketService]", "Server: Disconnect()");
            foreach (var connection in ServerClientConnections) {
                Logger.Log("[SocketService]", "Server: Disconnect:", connection.Name);
                var serverStreamReader = connection.Reader;
                var serverStreamWriter = connection.Writer;
                var serverRemoteSocket = connection.Socket;
                
                if (serverStreamReader != null) {
                    try {
                        serverStreamReader.DetachStream();
                        serverStreamReader.Dispose();
                    } catch (Exception ex) {
                        Logger.Log("[SocketService]", "Exception while releasing _serverStreamReader:", ex.ToString(), logLevel: LogLevel.Error);
                    }
                }

                if (serverStreamWriter != null) {
                    try {
                        serverStreamWriter.DetachStream();
                        serverStreamWriter.Dispose();
                    } catch (Exception ex) {
                        Logger.Log("[SocketService]", "Exception while releasing _serverStreamWriter:", ex.ToString(), logLevel: LogLevel.Error);
                    }
                }

                if (serverRemoteSocket != null) {
                    try {
                        serverRemoteSocket.Dispose();
                    } catch (Exception ex) {
                        Logger.Log("[SocketService]", "Exception while disposing _serverRemoteSocket:", ex.ToString(), logLevel: LogLevel.Error);
                    }
                }
            }
            
            _serverClientConnectionsDict.Clear();
            
            Logger.Log("[SocketService]", "Server: Disconnected");
        }
        
        public async Task ServerForwardMessage(RemoteDevice senderDevice, string sendString) {
            Logger.Log("[SocketService]", $"Server: ServerSendStringToAll(\"{sendString.Truncate(100, "...")}\")", logLevel: LogLevel.Verbose);
            await Task.Run(() => {
                var receiverDevices = _serverClientConnectionsDict.Where(itm => itm.Key != senderDevice);
                Parallel.ForEach(receiverDevices, async connection => {
                    var serverStreamWriter = connection.Value.Writer;
                
                    try {
                        serverStreamWriter.WriteUInt32((uint) sendString.Length);
                        serverStreamWriter.WriteString(sendString);
                        await serverStreamWriter.StoreAsync();
                    } catch (Exception ex) {
                        Logger.Log("[SocketService]", "ServerSendStringToAll Exception:", ex.ToString(), logLevel: LogLevel.Error);
                    }
                });
            });
            
            Logger.Log("[SocketService]", "Server: ServerSendStringToAll: Sent", logLevel: LogLevel.Verbose);
        }

        public async Task ServerSendStringToClient(RemoteDevice remoteDevice, string sendString) {
            await ServerSendStringToClient(_serverClientConnectionsDict[remoteDevice], sendString);
        }
        
        public static async Task ServerSendStringToClient(ServerClientConnection serverClientConnection, string sendString) {
            Logger.Log("[SocketService]", $"Server: ServerSendStringToClient({serverClientConnection.Name}, \"{sendString.Truncate(100, "...")}\")", logLevel: LogLevel.Verbose);
            var serverStreamWriter = serverClientConnection.Writer;
        
            try {
                serverStreamWriter.WriteUInt32((uint) sendString.Length);
                serverStreamWriter.WriteString(sendString);
                await serverStreamWriter.StoreAsync();
            } catch (Exception ex) {
                Logger.Log("[SocketService]", "ServerSendStringToAll Exception:", ex.ToString(), logLevel: LogLevel.Error);
            }
            
            Logger.Log("[SocketService]", "Server: ServerSendStringToAll: Sent", logLevel: LogLevel.Verbose);
        }
        
        public async Task<string> ServerReceiveStringFromClient(RemoteDevice remoteDevice) {
            return await ServerReceiveStringFromClient(_serverClientConnectionsDict[remoteDevice]);
        }
        
        public async Task<string> ServerReceiveStringFromClient(ServerClientConnection serverClientConnection) {
            Logger.Log("[SocketService]", $"Server: ServerReceiveStringFromClient {serverClientConnection.Name}", logLevel: LogLevel.Verbose);
            var serverStreamReader = serverClientConnection.Reader;
            
            try {
                var size = await serverStreamReader.LoadAsync(sizeof(uint));
                if (size < sizeof(uint)) {
                    ServerDisconnectAllClients();
                }

                var stringLength = serverStreamReader.ReadUInt32();
                var actualStringLength = await serverStreamReader.LoadAsync(stringLength);
                if (actualStringLength != stringLength) {
                    Logger.Log("[SocketService]", "Server: Socket closed", logLevel: LogLevel.Warning);
                    ServerDisconnectAllClients();
                }

                var receivedString = serverStreamReader.ReadString(stringLength);
                Logger.Log("[SocketService]", "Received:", receivedString.Truncate(100, "..."), logLevel: LogLevel.Verbose);
                return receivedString;
            } catch (Exception ex) {
                Logger.Log("[SocketService]", "ServerReceiveStringFromAny Exception:", ex.ToString(), logLevel: LogLevel.Error);
                return null;
            }
        }
        
        private async void OnBtConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args) {
            Logger.Log("[SocketService]", "Server: OnBtConnectionReceived");
            StreamSocket serverRemoteSocket;
            try {
                serverRemoteSocket = args.Socket;
            } catch (Exception ex) {
                Logger.Log("[SocketService]", "Server: OnBtConnectionReceived - Error in GetSocket", ex, logLevel: LogLevel.Error);
                return;
            }

            var serverRemoteDevice = await BluetoothDevice.FromHostNameAsync(serverRemoteSocket.Information.RemoteHostName);

            var serverStreamWriter = new DataWriter(serverRemoteSocket.OutputStream);
            var serverStreamReader = new DataReader(serverRemoteSocket.InputStream);

            var remoteDevice = new RemoteDevice(serverRemoteDevice.DeviceId, serverRemoteDevice.Name);
            var connection = new ServerClientConnection(serverRemoteDevice, serverRemoteSocket, serverStreamWriter, serverStreamReader);
            
            _serverClientConnectionsDict.Add(remoteDevice, connection);
            
            _connectionReceivedCallback?.Invoke(remoteDevice);
            Logger.Log("[SocketService]", "Server: Connected to Client: " + serverRemoteDevice.Name);
        }

        private const string BluetoothAqs = "(System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\")";
        private static readonly string[] BluetoothDeviceProperties = {"System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected"};
        private const DeviceInformationKind BluetoothDeviceKind = DeviceInformationKind.AssociationEndpoint;
        private static readonly DeviceWatcher BluetoothDeviceWatcher = DeviceInformation.CreateWatcher(BluetoothAqs, BluetoothDeviceProperties, BluetoothDeviceKind);
        private static readonly Dictionary<string, RemoteDevice> BluetoothDevices = new Dictionary<string, RemoteDevice>();

        private static bool _bluetoothDeviceWatcherInitialized;
        private static Action _bluetoothDeviceWatcherStoppedCallback;
        private static Action _bluetoothDeviceWatcherEnumerationCompletedCallback;
        private void InitializeBluetoothDeviceWatcher(Action stoppedCallback = null, Action enumerationCompletedCallback = null) {
            _bluetoothDeviceWatcherStoppedCallback = stoppedCallback;
            _bluetoothDeviceWatcherEnumerationCompletedCallback = enumerationCompletedCallback;
            
            BluetoothDeviceWatcher.Stopped += OnBluetoothDeviceWatcherStopped;
            BluetoothDeviceWatcher.EnumerationCompleted += OnBluetoothDeviceEnumerationCompleted;

            BluetoothDeviceWatcher.Added += OnBluetoothDeviceAdded;
            BluetoothDeviceWatcher.Removed += OnBluetoothDeviceRemoved;

            _bluetoothDeviceWatcherInitialized = true;
        }

        private void OnBluetoothDeviceWatcherStopped(DeviceWatcher sender, object args) {
            Logger.Log("[SocketService]", "BluetoothDeviceWatcher Stopped");
            _bluetoothDeviceWatcherStoppedCallback?.Invoke();
        }

        private void OnBluetoothDeviceEnumerationCompleted(DeviceWatcher sender, object args) {
            Logger.Log("[SocketService]", "BluetoothDeviceWatcher EnumerationCompleted");
            _bluetoothDeviceWatcherEnumerationCompletedCallback?.Invoke();
        }

        private void OnBluetoothDeviceAdded(DeviceWatcher sender, DeviceInformation args) {
            Logger.Log("[SocketService]", "BluetoothDeviceWatcher added", args.Id, $"({args.Name})");
            var device = new RemoteDevice(args.Id, args.Name);
            if (!BluetoothDevices.ContainsKey(args.Id)) {
                BluetoothDevices.Add(args.Id, device);
            }
        }

        private void OnBluetoothDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate args) {
            Logger.Log("[SocketService]", "BluetoothDeviceWatcher removed", args.Id);
            if (BluetoothDevices.ContainsKey(args.Id)) {
                BluetoothDevices.Remove(args.Id);
            }
        }

        private readonly List<TypedEventHandler<DeviceWatcher, DeviceInformation>> _btDeviceAddedListener = new List<TypedEventHandler<DeviceWatcher, DeviceInformation>>();
        private readonly List<TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>> _btDeviceRemovedListener = new List<TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>>();

        public void StartBluetoothDeviceEnumeration(Action<string, RemoteDevice> callback, bool removeOldListeners = true) {
            StopBluetoothDeviceEnumeration();

            if (removeOldListeners) {
                foreach (var oldAddedListener in _btDeviceAddedListener) {
                    BluetoothDeviceWatcher.Added -= oldAddedListener;
                }

                foreach (var oldRemovedListener in _btDeviceRemovedListener) {
                    BluetoothDeviceWatcher.Removed -= oldRemovedListener;
                }
            }

            var addedListener = new TypedEventHandler<DeviceWatcher, DeviceInformation>((_, args) => {
                var device = new RemoteDevice(args.Id, args.Name);
                callback("added", device);
            });
            if (BluetoothDeviceWatcher.Status is DeviceWatcherStatus.Started or DeviceWatcherStatus.EnumerationCompleted) BluetoothDeviceWatcher.Stop();
            try {
                BluetoothDeviceWatcher.Added += addedListener;
                _btDeviceAddedListener.Add(addedListener);
            } catch (InvalidOperationException ex) {
                Logger.Log("[SocketService]", "Subscribing to BluetoothDeviceWatcher.Added failed:", ex, logLevel: LogLevel.Error);
            }

            var removedListener = new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>((_, args) => {
                var device = BluetoothDevices.ContainsKey(args.Id) ? BluetoothDevices[args.Id] : new RemoteDevice(args.Id, $"Unknown (ID: {args.Id})");
                callback("removed", device);
            });
            try {
                BluetoothDeviceWatcher.Removed += removedListener;
                _btDeviceRemovedListener.Add(removedListener);
            } catch (InvalidOperationException ex) {
                Logger.Log("[SocketService]", "Subscribing to BluetoothDeviceWatcher.Removed failed:", ex, logLevel: LogLevel.Error);
            }

            if (!_bluetoothDeviceWatcherInitialized) {
                InitializeBluetoothDeviceWatcher();
            }

            BluetoothDeviceWatcher.Start();
        }

        public void StopBluetoothDeviceEnumeration() {
            if (BluetoothDeviceWatcher.Status != DeviceWatcherStatus.Started && BluetoothDeviceWatcher.Status != DeviceWatcherStatus.EnumerationCompleted) return;
            BluetoothDeviceWatcher.Stop();
        }

        private StreamSocket _clientRemoteSocket;
        private DataWriter _clientStreamWriter;
        private DataReader _clientStreamReader;
        
        public async Task ClientSendString(string message) {
            try {
                Logger.Log($"Client: Sending {message}");
                _clientStreamWriter.WriteUInt32((uint) message.Length);
                _clientStreamWriter.WriteString(message);
                await _clientStreamWriter.StoreAsync();
                Logger.Log("Client: Sent");
            } catch (Exception ex) {
                Logger.Log("[SocketService]", "ClientSendString Exception:", ex.ToString(), logLevel: LogLevel.Error);
            }
        }
        
        
        public async Task<string> ClientReceiveString() {
            try {
                Logger.Log("Client: ReceiveString: Receiving", logLevel: LogLevel.Verbose);
                var size = await _clientStreamReader.LoadAsync(sizeof(uint));
                if (size < sizeof(uint)) {
                    throw new Exception("Client: Socket disconnected");
                }

                var stringLength = _clientStreamReader.ReadUInt32();
                var actualStringLength = await _clientStreamReader.LoadAsync(stringLength);
                if (actualStringLength != stringLength) {
                    throw new Exception("Client: Socket closed");
                }

                var receivedString = _clientStreamReader.ReadString(stringLength);
                Logger.Log("Client: Received:", receivedString.Truncate(100, "..."), logLevel: LogLevel.Verbose);
                return receivedString;
            } catch (Exception ex) {
                Logger.Log("[SocketService]", "ClientReceiveString Exception:", ex.ToString(), logLevel: LogLevel.Error);
                return null;
            }
        }

        public async Task<bool> ClientConnectToServer(RemoteDevice server) {
            try {
                Logger.Log("[SocketService]", "BTClient: ", "Connecting...");
                var bluetoothDevice = await BluetoothDevice.FromIdAsync(server.Id);
                var rfcommServices = await bluetoothDevice.GetRfcommServicesForIdAsync(RfcommServiceId.FromUuid(Configuration.RfcommChatServiceUuid), BluetoothCacheMode.Uncached);
                Logger.Log("[SocketService]", $"BTClient: Found {rfcommServices.Services.Count} remote services");

                RfcommDeviceService remoteService;
                if (rfcommServices.Services.Count > 0) {
                    remoteService = rfcommServices.Services[0];
                    Logger.Log("Client: Service found:", remoteService.ServiceId.AsString(), remoteService.ConnectionServiceName);
                } else {
                    Logger.Log("Client: Service not running on target device", rfcommServices.Services.Count);
                    Logger.Log("Error", rfcommServices.Error);
                    RestartBluetoothServer();
                    return false;
                }

                var serviceAttributes = await remoteService.GetSdpRawAttributesAsync();
                if (!serviceAttributes.ContainsKey(Configuration.SdpServiceNameAttributeId)) {
                    Logger.Log("Client: Service not advertising", Configuration.SdpServiceNameAttributeId);
                    remoteService.Dispose();
                    RestartBluetoothServer();
                    return false;
                }

                var attributeReader = DataReader.FromBuffer(serviceAttributes[Configuration.SdpServiceNameAttributeId]);
                var attributeType = attributeReader.ReadByte();
                if (attributeType != Configuration.SdpServiceNameAttributeType) {
                    Logger.Log("Client: Unexpected format");
                    remoteService.Dispose();
                    RestartBluetoothServer();
                    return false;
                }

                var serviceNameLength = attributeReader.ReadByte();
                attributeReader.UnicodeEncoding = UnicodeEncoding.Utf8;

                // connect
                _clientRemoteSocket = new StreamSocket();
                await _clientRemoteSocket.ConnectAsync(remoteService.ConnectionHostName, remoteService.ConnectionServiceName);

                var serviceName = attributeReader.ReadString(serviceNameLength);
                Logger.Log("Client: Connected to", serviceName, "on", server.Name);
                _clientStreamWriter = new DataWriter(_clientRemoteSocket.OutputStream);
                _clientStreamReader = new DataReader(_clientRemoteSocket.InputStream);
                return true;
            } catch (Exception e) {
                Logger.Log("ClientConnectToServer", e, logLevel: LogLevel.Error);
                return false;
            }
        }
        
        public void ClientDisconnectServer() {
            _clientStreamWriter.Dispose();
            _clientStreamReader.Dispose();
            _clientRemoteSocket.Dispose();
            Logger.Log("Client: Disconnected");
        }
    }
}