using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AINotes.Helpers.Imaging;
using Helpers;
using Helpers.Controls;
using Helpers.Essentials;
using Helpers.Extensions;
using MaterialComponents;
using ColumnDefinition = Windows.UI.Xaml.Controls.ColumnDefinition;

namespace AINotes.Screens {
    public partial class FeedbackScreen {
        public override void OnLoad() {
            base.OnLoad();
            LoadToolbar();
        }

        public async void OnSendButtonPressed(object sender, RoutedEventArgs args) {
            var includeLogs = IncludeLogsCheckBox.IsChecked ?? false;
        
            // disable everything
            SendButton.Text = includeLogs ? "Preparing logs" : "SendingPleaseWait";
            SendButton.IsEnabled = false;
            IncludeLogsCheckBox.IsEnabled = false;
            FeedbackField.IsEnabled = false;

            await SendFeedback(FeedbackField.Text, includeLogs);
            await Task.Delay(1000);
            
            // reset
            SendButton.Text = "SendFeedback";
            FeedbackField.Text = "";
            SendButton.IsEnabled = true;
            IncludeLogsCheckBox.IsEnabled = true;
            FeedbackField.IsEnabled = true;
        }
        
        public FeedbackScreen() {
            InitializeComponent();
                
            // generate ui
            FeedbackField.Style = (Style) Application.Current.Resources["TextBoxWithoutDelete"];
            UserIDLabel.Text = "UserID: " + SystemInfo.GetSystemId();

            ContactOptionsPanel.Children.AddRange(new List<UIElement> {
                new MDLabel { Text = @"Team", FontSize = 22 },
                GenerateContactList(Configuration.Contact.Team),
                
                new MDLabel { Text = @"Vincent", FontSize = 22 },
                GenerateContactList(Configuration.Contact.Vincent),
                
                new MDLabel { Text = @"Fabian", FontSize = 22 },
                GenerateContactList(Configuration.Contact.Fabian),
            });
        }

        private StackPanel GenerateContactList(Dictionary<Configuration.Contact.Media, string> contactDict) {
            var contactKeys = contactDict.Keys.ToList();
            var contacts = (from key in contactDict.Select((_, i) => contactKeys[i]) let iconSource = Configuration.Contact.Icons[key] select (ImageSourceHelper.FromName(iconSource), contactDict[key])).ToList();
            var contactList = new StackPanel {Height = contactKeys.Count * 36 + 16};

            foreach (var (src, txt) in contacts) {
                var frm = new CustomFrame {
                    Content = new Grid {
                        Height = 36,
                        BorderThickness = new Thickness(0),
                        CornerRadius = new CornerRadius(0),
                        ColumnSpacing = 0,
                        ColumnDefinitions = {new ColumnDefinition {Width = new GridLength(36)}, new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)},},
                        Children = {
                            {
                                new Image {
                                    Width = 24, Height = 24, Margin = new Thickness(6), Source = src,
                                },
                                0, 0
                            },
                            {new MDLabel {Height = 24, Margin = new Thickness(6), Text = txt}, 0, 1}
                        }
                    }
                };
                contactList.Children.Add(frm);
                frm.Touch += (_, args) => OnContactItemTouched(frm, txt, args);
            }

            return contactList;
        }

        private void OnContactItemTouched(CustomFrame frm, string txt, WTouchEventArgs args) {
            switch (args.ActionType) {
                case WTouchAction.Entered:
                case WTouchAction.Released:
                    MainThread.BeginInvokeOnMainThread(() => frm.Background = Configuration.Theme.ToolbarItemHover);
                    break;
                case WTouchAction.Pressed:
                    MainThread.BeginInvokeOnMainThread(() => frm.Background = Configuration.Theme.ToolbarItemTap);
                    break;
                case WTouchAction.Cancelled:
                case WTouchAction.Exited:
                    MainThread.BeginInvokeOnMainThread(() => frm.Background = Configuration.Theme.Background);
                    break;
            }

            if (args.ActionType != WTouchAction.Released || args.MouseButton != WMouseButton.Left) return;
            OpenContact(txt);
        }
    }
}