using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Networking.Connectivity;
using Helpers.Essentials;

namespace Helpers.Networking {
    public enum ConnectionProfile {
        Unknown = 0,
        // Bluetooth = 1,
        Cellular = 2,
        Ethernet = 3,
        WiFi = 4
    }

    public enum NetworkAccess {
        Unknown = 0,
        None = 1,
        Local = 2,
        ConstrainedInternet = 3,
        Internet = 4
    }

    public static class Connectivity {
        private static event EventHandler<ConnectivityChangedEventArgs> ConnectivityChangedInternal;
        private static NetworkAccess _currentAccess;
        private static List<ConnectionProfile> _currentProfiles;

        public static NetworkAccess NetworkAccess => PlatformNetworkAccess;
        public static IEnumerable<ConnectionProfile> ConnectionProfiles => PlatformConnectionProfiles.Distinct();

        // ReSharper disable once EventNeverSubscribedTo.Global
        public static event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged {
            add {
                var wasRunning = ConnectivityChangedInternal != null;

                ConnectivityChangedInternal += value;

                if (!wasRunning && ConnectivityChangedInternal != null) {
                    SetCurrent();
                    StartListeners();
                }
            }

            remove {
                var wasRunning = ConnectivityChangedInternal != null;

                ConnectivityChangedInternal -= value;

                if (wasRunning && ConnectivityChangedInternal == null) StopListeners();
            }
        }

        private static void SetCurrent() {
            _currentAccess = NetworkAccess;
            _currentProfiles = new List<ConnectionProfile>(ConnectionProfiles);
        }

        private static void OnConnectivityChanged(NetworkAccess access, IEnumerable<ConnectionProfile> profiles) => OnConnectivityChanged(new ConnectivityChangedEventArgs(access, profiles));

        public static void OnConnectivityChanged() => OnConnectivityChanged(NetworkAccess, ConnectionProfiles);

        private static void OnConnectivityChanged(ConnectivityChangedEventArgs e) {
            if (_currentAccess == e.NetworkAccess && _currentProfiles.SequenceEqual(e.ConnectionProfiles)) return;
            SetCurrent();
            MainThread.BeginInvokeOnMainThread(() => ConnectivityChangedInternal?.Invoke(null, e));
        }

        private static void StartListeners() => NetworkInformation.NetworkStatusChanged += NetworkStatusChanged;

        private static void NetworkStatusChanged(object sender) => OnConnectivityChanged();

        private static void StopListeners() => NetworkInformation.NetworkStatusChanged -= NetworkStatusChanged;

        private static NetworkAccess PlatformNetworkAccess {
            get {
                var profile = NetworkInformation.GetInternetConnectionProfile();
                if (profile == null) return NetworkAccess.Unknown;

                var level = profile.GetNetworkConnectivityLevel();
                switch (level) {
                    case NetworkConnectivityLevel.LocalAccess:
                        return NetworkAccess.Local;
                    case NetworkConnectivityLevel.InternetAccess:
                        return NetworkAccess.Internet;
                    case NetworkConnectivityLevel.ConstrainedInternetAccess:
                        return NetworkAccess.ConstrainedInternet;
                    default:
                        return NetworkAccess.None;
                }
            }
        }

        private static IEnumerable<ConnectionProfile> PlatformConnectionProfiles {
            get {
                var networkInterfaceList = NetworkInformation.GetConnectionProfiles();
                foreach (var interfaceInfo in networkInterfaceList.Where(nii => nii.GetNetworkConnectivityLevel() != NetworkConnectivityLevel.None)) {
                    var type = ConnectionProfile.Unknown;

                    if (interfaceInfo.NetworkAdapter != null) {
                        // http://www.iana.org/assignments/ianaiftype-mib/ianaiftype-mib
                        switch (interfaceInfo.NetworkAdapter.IanaInterfaceType) {
                            case 6:
                                type = ConnectionProfile.Ethernet;
                                break;
                            case 71:
                                type = ConnectionProfile.WiFi;
                                break;
                            case 243:
                            case 244:
                                type = ConnectionProfile.Cellular;
                                break;

                            case 281:
                                continue;
                        }
                    }

                    yield return type;
                }
            }
        }
    }


    public class ConnectivityChangedEventArgs : EventArgs {
        public ConnectivityChangedEventArgs(NetworkAccess access, IEnumerable<ConnectionProfile> connectionProfiles) {
            NetworkAccess = access;
            ConnectionProfiles = connectionProfiles;
        }

        public NetworkAccess NetworkAccess { get; }

        public IEnumerable<ConnectionProfile> ConnectionProfiles { get; }

        public override string ToString() => $"{nameof(NetworkAccess)}: {NetworkAccess}, " + $"{nameof(ConnectionProfiles)}: [{string.Join(", ", ConnectionProfiles)}]";
    }
}