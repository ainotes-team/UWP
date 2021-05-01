using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AINotes.Models;
using Helpers;

namespace AINotes.Helpers.PreferenceHelpers {
    public class ComponentListPreference : Preference {
        public override UIElement GetView() => View;
        private ListView _view;

        public ListView View =>
            _view ??= new ListView {
                ItemsSource = _extensionModels,
                SelectionMode = ListViewSelectionMode.None,
                Background = Configuration.Theme.Background,
                // ItemTemplate = new DataTemplate(() => {
                //     var nameLabel = new CustomLabel {
                //         VerticalTextAlignment = TextAlignment.Center,
                //         HeightRequest = 36,
                //         Margin = new Thickness(6, 0, 0, 0)
                //     };
                //     nameLabel.SetBinding(Label.TextProperty, "DisplayName");
                //     var extensionLogo = new CustomCachedImage {
                //         HeightRequest = 24,
                //         WidthRequest = 24,
                //         Margin = 6
                //     };
                //     extensionLogo.SetBinding(CachedImage.SourceProperty, "LogoImageSource");
                //
                //
                //     var idLabel = new CustomLabel {
                //         FontSize = 0,
                //         HeightRequest = 0
                //     };
                //     idLabel.SetBinding(Label.TextProperty, "UniqueId");
                //
                //     var s = new StackLayout {
                //         Orientation = StackOrientation.Horizontal,
                //         Children = {
                //             extensionLogo,
                //             nameLabel,
                //             idLabel
                //         }
                //     };
                //
                //     var contentFrame = new CustomFrame {
                //         Padding = 0, Margin = 0,
                //         InputTransparent = false, CascadeInputTransparent = false,
                //         Content = s,
                //     };
                //
                //     contentFrame.Touch += (sender, args) => {
                //         if (args.ActionType != SKTouchAction.Released || args.MouseButton != SKMouseButton.Right) return;
                //         var senderFrame = (CustomFrame) sender;
                //         var (x, y) = senderFrame.GetAbsoluteCoordinates();
                //         var (tX, tY) = args.Location.ToFormsPoint();
                //         App.Page.ShowDropdown(new List<CustomDropdownViewTemplate> {
                //             new CustomDropdownItem("Disable", async () => {
                //                 App.Page.CloseDropdown();
                //                 Logger.Log("Disable", idLabel.Text);
                //                 await _extensionManager.DisableExtensionAsync(_extensionManager.GetExtensionModel(idLabel.Text));
                //             }),
                //             new CustomDropdownItem("Uninstall", async () => {
                //                 App.Page.CloseDropdown();
                //                 Logger.Log("Uninstall", idLabel.Text);
                //                 await _extensionManager.RemoveExtensionAsync(_extensionManager.GetExtensionModel(idLabel.Text));
                //             }),
                //         }, new Point(x + tX, y + tY));
                //     };
                //     return new ViewCell {
                //         View = contentFrame,
                //     };
                // })
            };

        private readonly ObservableCollection<ExtensionModel> _extensionModels = new ObservableCollection<ExtensionModel>();
        private readonly ExtensionManager _extensionManager = new ExtensionManager();

        public ComponentListPreference(string displayName) : base("externalComponents", displayName, null) {
            Task.Run(async () => {
                if (!_extensionManager.IsInitialized()) await _extensionManager.InitializeAsync();
                _extensionManager.ExtensionsChanged += UpdateCollection;
                UpdateCollection();
            });
        }

        private void UpdateCollection() {
            // Logger.Log("[ComponentListPreference]", "UpdateCollection");
            _extensionModels.Clear();
            foreach (var extensionModel in _extensionManager.GetExtensionModels()) {
                Logger.Log("Extension:", extensionModel, extensionModel.DisplayName, extensionModel.Description);
                _extensionModels.Add(extensionModel);
            }

            View.ItemsSource = _extensionModels;
        }

        public static implicit operator List<ExtensionModel>(ComponentListPreference x) => x._extensionModels.ToList();
    }
}