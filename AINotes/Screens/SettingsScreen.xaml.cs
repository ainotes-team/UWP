using System.Collections.Generic;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AINotes.Helpers.PreferenceHelpers;
using Helpers.Extensions;
using MaterialComponents;

namespace AINotes.Screens {
    public partial class SettingsScreen {
        public override void OnLoad() {
            base.OnLoad();
            LoadToolbar();
        }

        public SettingsScreen() {
            InitializeComponent();
            ReloadSettings();
        }

        private string _currentGroupName;
        private string _lastGroupName;
        private void LoadSettingsGroup(string groupName, IEnumerable<Preference> groupContent) {
            if (groupName == _currentGroupName) return;
            MainScroll.ChangeView(0, 0, 1);
            
            if (groupName == _lastGroupName) {
                MainStackGrid.Children.Move((uint) (MainStackGrid.Children.Count - 2), (uint) (MainStackGrid.Children.Count - 1));
            } else {
                var mainStack = new StackPanel {
                    Padding = new Thickness(5),
                    Background = Configuration.Theme.Background     
                };
                foreach (var pref in groupContent) {
                    mainStack.Children.Add(new MDLabel {Text = pref.GetDisplayName(), FontSize = 20});
                    mainStack.Children.Add(pref.GetView());
                    mainStack.Children.Add(new Frame {Height = 10});
                }

                MainStackGrid.AddChild(mainStack, 0, 0);
            }

            if (MainStackGrid.Children.Count > 1) {
                foreach (var oldView in MainStackGrid.Children.Reverse().Skip(2)) {
                    if (oldView is Panel p) {
                        p.Children.Clear();
                    }
                    MainStackGrid.Children.Remove(oldView);
                }
            }

            _lastGroupName = _currentGroupName;
            _currentGroupName = groupName;
        }

        public void ReloadSettings() {
            MainStackGrid.Children.Clear();
            SideStack.Children.Clear();

            var settings = Preferences.GetSettings();
            foreach (var (groupName, groupContent) in settings) {
                Button groupButton;
                SideStack.Children.Add(groupButton = new MDButton {
                    Text = groupName,
                    ButtonStyle = MDButtonStyle.Custom,
                    Background = Colors.White.ToBrush(),
                    BorderBrush = ColorCreator.FromHex("#DADCE0").ToBrush(),
                    Foreground = Configuration.Theme.Text,
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    CornerRadius = new CornerRadius(0),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                });

                groupButton.Click += (_, _) => LoadSettingsGroup(groupName, groupContent);
            }

            LoadSettingsGroup(settings.First().Key, settings.First().Value);
        }
    }
}