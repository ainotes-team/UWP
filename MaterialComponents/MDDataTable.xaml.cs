using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Helpers;
using Helpers.Essentials;
using Helpers.Extensions;
using Helpers.Lists;

namespace MaterialComponents {
    public enum SortByMode {
        Alphabetical,
        LastEdited,
        LastCreated,
        Label,
        Owner,
        Status,
    }
    
    public interface IMDDataModel {
        string Name { get; set; }
        long LastChangedDate { get; set; }
        long CreationDate { get; set; }
        ObservableList<int> Labels { get; }
        string Owner { get; set; }
        string Status { get; set; }
        bool IsFavorite { get; set; }
    }

    public class MDDataTemplateSelector : DataTemplateSelector {
        public static readonly DependencyProperty TypeTemplatesProperty = DependencyProperty.Register(
            nameof(TypeTemplates), 
            typeof(Dictionary<Type, DataTemplate>),
            typeof(MDDataTemplateSelector),
            PropertyMetadata.Create("")
        );
        
        public Dictionary<Type, DataTemplate> TypeTemplates { get; set; }

        protected override DataTemplate SelectTemplateCore(object item) {
            var itemType = item.GetType();
            if (!TypeTemplates.ContainsKey(itemType)) throw new ArgumentOutOfRangeException(nameof(item), item, null);
            return TypeTemplates[itemType];
        }
    }
    
    // TODO: Headers Property / Generate Headers
    public partial class MDDataTable {
        public List<IMDDataModel> SelectedModels => ItemGrid.SelectedItems.Cast<IMDDataModel>().ToList();
        public ObservableList<IMDDataModel> ModelCollection = new ObservableList<IMDDataModel>();

        public event Action<object> ItemDoubleTapped;
        public event Action<DependencyObject, IMDDataModel, Point> ItemContextRequested;
        
        public static readonly DependencyProperty TypeTemplatesProperty = DependencyProperty.Register(
            nameof(TypeTemplates), 
            typeof(Dictionary<Type, DataTemplate>),
            typeof(MDDataTable),
            PropertyMetadata.Create("")
        );
        
        public Dictionary<Type, DataTemplate> TypeTemplates { get; set; }

        public MDDataTable() {
            InitializeComponent();
        }

        private void OnHeaderLoaded(object o, RoutedEventArgs routedEventArgs) {
            ToggleSortByNameTBI.PointerEntered += (sender, args) => ToggleSortByNameTBI.Opacity = 1.0;
            ToggleSortByNameTBI.PointerExited += (sender, args) => ToggleSortByNameTBI.Opacity = SortByMode == SortByMode.Alphabetical ? 1.0 : 0.0;
            ToggleSortByNameTBI.Pressed += (sender, args) => {
                SortBy(SortByMode.Alphabetical, SortByMode != SortByMode.Alphabetical || !SortByModeDescending);
            };
            
            ToggleSortByOwnerTBI.PointerEntered += (sender, args) => ToggleSortByOwnerTBI.Opacity = 1.0;
            ToggleSortByOwnerTBI.PointerExited += (sender, args) => ToggleSortByOwnerTBI.Opacity = SortByMode == SortByMode.Owner ? 1.0 : 0.0;
            ToggleSortByOwnerTBI.Pressed += (sender, args) => {
                SortBy(SortByMode.Owner, SortByMode != SortByMode.Owner || !SortByModeDescending);
            };
            
            ToggleSortByStatusTBI.PointerEntered += (sender, args) => ToggleSortByStatusTBI.Opacity = 1.0;
            ToggleSortByStatusTBI.PointerExited += (sender, args) => ToggleSortByStatusTBI.Opacity = SortByMode == SortByMode.Status ? 1.0 : 0.0;
            ToggleSortByStatusTBI.Pressed += (sender, args) => {
                SortBy(SortByMode.Status, SortByMode != SortByMode.Status || !SortByModeDescending);
            };
            
            ToggleSortByCreatedTBI.PointerEntered += (sender, args) => ToggleSortByCreatedTBI.Opacity = 1.0;
            ToggleSortByCreatedTBI.PointerExited += (sender, args) => ToggleSortByCreatedTBI.Opacity = SortByMode == SortByMode.LastCreated ? 1.0 : 0.0;
            ToggleSortByCreatedTBI.Pressed += (sender, args) => {
                SortBy(SortByMode.LastCreated, SortByMode != SortByMode.LastCreated || !SortByModeDescending);
            };
            
            ToggleSortByLabelsTBI.PointerEntered += (sender, args) => ToggleSortByLabelsTBI.Opacity = 1.0;
            ToggleSortByLabelsTBI.PointerExited += (sender, args) => ToggleSortByLabelsTBI.Opacity = SortByMode == SortByMode.Label ? 1.0 : 0.0;
            ToggleSortByLabelsTBI.Pressed += (sender, args) => {
                SortBy(SortByMode.Label, SortByMode != SortByMode.Label || !SortByModeDescending);
            };
        }
        
        private SortByMode _sortByMode;
        public SortByMode SortByMode {
            get => _sortByMode;
            set {    
                _sortByMode = value;
                ModelCollection.SortingSelector = i => i.GetType().ToString();
                switch (_sortByMode) {
                    case SortByMode.Alphabetical:
                        ModelCollection.SecondarySortingSelector = i => i.Name;
                        break;
                    case SortByMode.LastEdited:
                        ModelCollection.SecondarySortingSelector = i => i.LastChangedDate;
                        break;
                    case SortByMode.LastCreated:
                        ModelCollection.SecondarySortingSelector = i => i.CreationDate;
                        break;
                    case SortByMode.Label:
                        ModelCollection.SecondarySortingSelector = i => i.Labels.Count != 0 ? i.Labels.Min() : 0;
                        break;
                    case SortByMode.Owner:
                        ModelCollection.SecondarySortingSelector = i => i.Owner;
                        break;
                    case SortByMode.Status:
                        ModelCollection.SecondarySortingSelector = i => i.Status;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }
        }
        
        public bool SortByModeDescending {
            get => ModelCollection.Descending;
            set => ModelCollection.Descending = value;
        }
        
        public void SortBy(SortByMode mode, bool descending) {
            Logger.Log("[MDDataTable]", "SortBy", mode, descending);
            
            SortByMode = mode;
            ModelCollection.Descending = descending;

            ItemGrid.ItemsSource = null;
            ItemGrid.ItemsSource = ModelCollection;

            Logger.Log(ModelCollection.ToFString());
            
            SavedStatePreferenceHelper.Set("MaterialDesign_FileContainerFileSortByMode", (int) mode);
            SavedStatePreferenceHelper.Set("MaterialDesign_FileContainerFileSortByModeDescending", descending);
        
            switch (SortByMode) {
                case SortByMode.Alphabetical:
                    MainThread.BeginInvokeOnMainThread(() => {
                        ToggleSortByStatusTBI.Opacity = 0.0;
                        ToggleSortByNameTBI.Opacity = 1.0;
                        ToggleSortByOwnerTBI.Opacity = 0.0;
                        ToggleSortByCreatedTBI.Opacity = 0.0;
                        ToggleSortByLabelsTBI.Opacity = 0.0;
                    });
                    break;
                case SortByMode.LastEdited:
                    break;
                case SortByMode.LastCreated:
                    MainThread.BeginInvokeOnMainThread(() => {
                        ToggleSortByStatusTBI.Opacity = 0.0;
                        ToggleSortByNameTBI.Opacity = 0.0;
                        ToggleSortByOwnerTBI.Opacity = 0.0;
                        ToggleSortByCreatedTBI.Opacity = 1.0;
                        ToggleSortByLabelsTBI.Opacity = 0.0;
                    });
                    break;
                case SortByMode.Label:
                    MainThread.BeginInvokeOnMainThread(() => {
                        ToggleSortByStatusTBI.Opacity = 0.0;
                        ToggleSortByNameTBI.Opacity = 0.0;
                        ToggleSortByOwnerTBI.Opacity = 0.0;
                        ToggleSortByCreatedTBI.Opacity = 0.0;
                        ToggleSortByLabelsTBI.Opacity = 1.0;
                    });
                    break;
                case SortByMode.Owner:
                    MainThread.BeginInvokeOnMainThread(() => {
                        ToggleSortByStatusTBI.Opacity = 0.0;
                        ToggleSortByNameTBI.Opacity = 0.0;
                        ToggleSortByOwnerTBI.Opacity = 1.0;
                        ToggleSortByCreatedTBI.Opacity = 0.0;
                        ToggleSortByLabelsTBI.Opacity = 0.0;
                    });
                    break;
                case SortByMode.Status:
                    MainThread.BeginInvokeOnMainThread(() => {
                        ToggleSortByStatusTBI.Opacity = 1.0;
                        ToggleSortByNameTBI.Opacity = 0.0;
                        ToggleSortByOwnerTBI.Opacity = 0.0;
                        ToggleSortByCreatedTBI.Opacity = 0.0;
                        ToggleSortByLabelsTBI.Opacity = 0.0;
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnWrapViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args) {
            if (sender is ItemsWrapGrid iwg) {
                iwg.ItemWidth = args.EffectiveViewport.Width;
            }

            if (ItemGrid.Header is Grid headerGrid) {
                headerGrid.Width = args.EffectiveViewport.Width;
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs args) {
            foreach (var addedItem in args.AddedItems) {
                var item = ItemGrid.ContainerFromItem(addedItem);

                if (!(item is GridViewItem selectedItem) || !(selectedItem.ContentTemplateRoot is Grid grid)) continue;
                ((ContentPresenter) VisualTreeHelper.GetParent(grid)).Background = ColorCreator.FromHex("#E8F0FE").ToBrush();

                var nameLabel = (MDLabel) grid.FindName("ContentName");
                if (nameLabel != null) nameLabel.Foreground = Colors.Navy.ToBrush();
            }

            foreach (var addedItem in args.RemovedItems) {
                var item = ItemGrid.ContainerFromItem(addedItem);

                if (!(item is GridViewItem selectedItem) || !(selectedItem.ContentTemplateRoot is Grid grid)) continue;
                var parent = ((ContentPresenter) VisualTreeHelper.GetParent(grid));
                
                if (parent != null) parent.Background = Colors.Transparent.ToBrush();

                var nameLabel = (MDLabel) grid.FindName("ContentName");
                if (nameLabel != null) nameLabel.Foreground = Theming.CurrentTheme.Text;
            }
        }

        private void OnItemContextRequested(UIElement sender, ContextRequestedEventArgs args) {
            args.TryGetPosition(Window.Current.Content, out var p);
            var container = GetParent(sender, 3);
            var correspondingModel = ItemGrid.ItemFromContainer(container) as IMDDataModel;
            ItemContextRequested?.Invoke(container, correspondingModel, p);
        }

        private void OnItemDoubleTapped(object s, DoubleTappedRoutedEventArgs args) {
            Logger.Log("[MDDataTable]", $"-> OnItemDoubleTapped ({s}, {args})");
            ItemDoubleTapped?.Invoke(ItemGrid.SelectedItem);
            args.Handled = true;
            Logger.Log("[MDDataTable]", "<- OnItemDoubleTapped");
        }

        private DependencyObject GetParent(DependencyObject obj) => VisualTreeHelper.GetParent(obj);
        private DependencyObject GetParent(DependencyObject obj, int idx) {
            for (var i = 0; i < idx; i++) {
                obj = GetParent(obj);
            }

            return obj;
        }
    }
}