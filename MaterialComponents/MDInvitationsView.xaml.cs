using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace MaterialComponents {
    public class Invitation {
        public string FileName { get; }
        public string UserFilePermissionId { get; }

        private string _permissionLevel;
        public string PermissionLevel {
            get {
                var convert = new Dictionary<string, string> {
                    {"2", "Owner"},
                    {"1", "Write"},
                    {"0", "Read-Only"},
                };

                return convert[_permissionLevel];
            }
            set => _permissionLevel = value;
        }
        
        public Invitation(string fileName, string userFilePermissionId, string permissionLevel) {
            FileName = fileName;
            UserFilePermissionId = userFilePermissionId;
            PermissionLevel = permissionLevel;
        }
    }
    
    public sealed partial class MDInvitationsView {
        public event Action<string> InvitationAccepted;
        public event Action<string> InvitationDeclined;

        public MDInvitationsView() {
            InitializeComponent();
        }

        public void SetInvitations(Invitation[] invitations) {
            InvitationsList.ItemsSource = null;
            InvitationsList.ItemsSource = invitations;
        }

        private void ItemPointerEntered(object sender, PointerRoutedEventArgs e) {
            ((Frame) sender).Background = new SolidColorBrush(Colors.LightGray);
        }

        private void ItemPointerExited(object sender, PointerRoutedEventArgs e) {
            ((Frame) sender).Background = new SolidColorBrush(Colors.Transparent);
        }


        private void InvitationAcceptedClick(object sender, RoutedEventArgs e) {
            var button = (MDButton) sender;
            var context = (Invitation) button.DataContext;
            InvitationAccepted?.Invoke(context.UserFilePermissionId);
        }

        private void InvitationDeclinedClick(object sender, RoutedEventArgs e) {
            var button = (MDButton) sender;
            var context = (Invitation) button.DataContext;
            
            InvitationDeclined?.Invoke(context.UserFilePermissionId);
        }
    }
}