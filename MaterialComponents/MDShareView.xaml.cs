using System;
using System.Collections.Generic;
using Windows.System;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Helpers.Extensions;

namespace MaterialComponents {
    public sealed partial class MDShareView {
        public event Action RemoteEntryEnterPressed;
        public string RemoteEntryText {
            get => RemoteEntry.Text;
            set => RemoteEntry.Text = value;
        }

        public IList<object> LocalSelectedDevices => LocalDevicesList.SelectedItems;

        public event Action<AccountItem, string> AccountPermissionDropdownChanged;
        public event Action<AccountItem> AccountPermissionDropdownRemoved;
        
        public event Action RemoteDoneButtonClick;
        public event Action LocalCancelButtonClick;
        public event Action LocalSendButtonClick;
        
        public event Action LocalLiveButtonClick;

        public MDShareView() {
            InitializeComponent();

            RemoteSharing.Background = LocalSharing.Background = Theming.CurrentTheme.Background;
            
            RemoteDoneButton.Click += OnRemoteDoneButtonClick;

            LocalCancelButton.Click += OnLocalCancelButtonClick;
            LocalSendButton.Click += OnLocalSendButtonClick;
            LocalLiveButton.Click += OnLocalLiveButtonClick;
        }
        
        private void OnRemoteDoneButtonClick(object sender, RoutedEventArgs e) => RemoteDoneButtonClick?.Invoke();
        
        private void OnLocalCancelButtonClick(object sender, RoutedEventArgs e) => LocalCancelButtonClick?.Invoke();
        private void OnLocalSendButtonClick(object sender, RoutedEventArgs e) => LocalSendButtonClick?.Invoke();
        private void OnLocalLiveButtonClick(object sender, RoutedEventArgs e) => LocalLiveButtonClick?.Invoke();

        public void SetRemotePermittedUsers(Dictionary<AccountItem, string> items) {
            var itemsWithOptions = new List<AccountItem>();
            var options = new Dictionary<string, string> {
                // {"2", "Owner"},
                {"1", "Write"},
                {"0", "Read-Only"},
                {"-1", "Remove"},
            };
            foreach (var (accountItem, permissionLevel) in items) {
                FrameworkElement c;
                if (permissionLevel == "2") {
                    c = new MDLabel {
                        Text = "Owner",
                        FontStyle = FontStyle.Italic
                    };
                } else {
                    c = new MDPicker();
                    ((MDPicker) c).ItemsSource = options.Values;
                    ((MDPicker) c).SelectedItem = options[permissionLevel];

                    void OnSelectionChanged(object sender, SelectionChangedEventArgs args) {
                        if ((string) ((MDPicker) c).SelectedItem == options["-1"]) {
                            AccountPermissionDropdownRemoved?.Invoke(accountItem);
                            ((MDPicker) c).SelectionChanged -= OnSelectionChanged;
                        } else {
                            AccountPermissionDropdownChanged?.Invoke(accountItem, options.ReverseLookup((string) ((MDPicker) c).SelectedItem));
                        }
                    }

                    ((MDPicker) c).SelectionChanged += OnSelectionChanged;
                }
                accountItem.ExtraOptionsView = new StackPanel {
                    Padding = new Thickness(0, 0, 10, 0),
                    Margin = new Thickness(0),
                    Spacing = 0,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children = {
                        c
                    }
                };
                itemsWithOptions.Add(accountItem);
            }
            RemoteAccountsList.SetItems(itemsWithOptions.ToArray());
        }
        
        public void SetRemoteEntryError(bool error) {
            RemoteEntry.Error = error;
        }

        private void OnRemoteEntryKeyDown(object sender, KeyRoutedEventArgs e) {
            if (e.Key == VirtualKey.Enter) {
                RemoteEntryEnterPressed?.Invoke();
            }
        }

        public void SetLocalDevices(List<object> devices) {
            LocalDevicesList.ItemsSource = devices;
        }
    }
}