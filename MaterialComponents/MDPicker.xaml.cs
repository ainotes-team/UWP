using System.Linq;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Helpers;
using Helpers.Extensions;

namespace MaterialComponents {
    public partial class MDPicker {
        private readonly Brush _borderBrush = ColorCreator.FromHex("#DADCE0").ToBrush();

        public object ItemsSource {
            get => InternalComboBox.ItemsSource;
            set => InternalComboBox.ItemsSource = value;
        }
        
        public object SelectedItem {
            get => InternalComboBox.SelectedItem;
            set => InternalComboBox.SelectedItem = value;
        }
        
        public ItemCollection Items => InternalComboBox.Items;

        public event SelectionChangedEventHandler SelectionChanged;
        
        public MDPicker() {
            InitializeComponent();

            InternalComboBox.BorderBrush = _borderBrush;
            Background = InternalComboBox.Background = Theming.CurrentTheme.Background;
            InternalComboBox.BorderThickness = new Thickness(1);
            InternalComboBox.MinWidth = 120;
            
            InternalComboBox.DropDownOpened += (_, __) => Open();
            InternalComboBox.SelectionChanged += (s, args) => SelectionChanged?.Invoke(s, args);
            
        }

        public void Open() {
            Popup popup = null;
            
            var childList = new ListView {
                ItemsSource = InternalComboBox.ItemsSource,
                SelectionMode = ListViewSelectionMode.Single,
                SelectedIndex = InternalComboBox.SelectedIndex,
                MaxHeight = 250
            };
        
            void OnChildListSelectionChanged(object _, SelectionChangedEventArgs __) {
                InternalComboBox.SelectedIndex = childList.SelectedIndex;
                if (popup == null) return;
                if (popup.IsOpen) popup.IsOpen = false;
                if (InternalComboBox.IsDropDownOpen) InternalComboBox.IsDropDownOpen = false;
            }
        
            childList.SelectionChanged += OnChildListSelectionChanged;
        
            var contentScroll = new ScrollViewer {
                Content = childList
            };
            
            var (x, y) = this.GetAbsoluteCoordinates();
            popup = new Popup {
                IsOpen = true,
                LightDismissOverlayMode = LightDismissOverlayMode.Off,
                IsLightDismissEnabled = true,
                HorizontalOffset = x,
                VerticalOffset = y-RenderSize.Height,
                Child = new Grid {
                    Background = Background,
                    Width = InternalComboBox.ActualWidth,
                    BorderThickness = new Thickness(1),
                    BorderBrush = _borderBrush,
                    CornerRadius = new CornerRadius(4),
                    Children = {
                        contentScroll
                    }
                }
            };
        
            static int Mod(int x, int m) {
                return (x%m + m)%m;
            }
        
            popup.PreviewKeyDown += (sender, e) => {
                Logger.Log("popup.PreviewKeyDown", e.Key);
                switch (e.Key) {
                    case VirtualKey.Down:
                        childList.SelectionChanged -= OnChildListSelectionChanged;
                        childList.SelectedIndex = Mod(childList.SelectedIndex + 1, childList.Items?.Count ?? 0);
                        InternalComboBox.SelectedIndex = childList.SelectedIndex;
                        childList.ScrollIntoView(childList.SelectedItem);
                        childList.SelectionChanged += OnChildListSelectionChanged;
                        e.Handled = true;
                        break;
                    case VirtualKey.Up:
                        childList.SelectionChanged -= OnChildListSelectionChanged;
                        childList.SelectedIndex = Mod(childList.SelectedIndex - 1, childList.Items?.Count ?? 0);
                        InternalComboBox.SelectedIndex = childList.SelectedIndex;
                        childList.ScrollIntoView(childList.SelectedItem);
                        childList.SelectionChanged += OnChildListSelectionChanged;
                        e.Handled = true;
                        break;
                    case VirtualKey.Enter:
                        OnChildListSelectionChanged(null, null);
                        e.Handled = true;
                        break;
                    default:
                        e.Handled = false;
                        break;
                }
            };
            popup.CharacterReceived += (sender, args) => {
                if (!popup.IsOpen) return;
                var select = childList.Items?.FirstOrDefault(item => item.ToString().ToLower().StartsWith(args.Character.ToString().ToLower()));
                childList.SelectionChanged -= OnChildListSelectionChanged;
                childList.SelectedItem = select;
                InternalComboBox.SelectedIndex = childList.SelectedIndex;
                childList.ScrollIntoView(childList.SelectedItem);
                childList.SelectionChanged += OnChildListSelectionChanged;
            };
            // ((CustomPicker) Element).OpenDropdown((position, text) => { Control.SelectedItem = Control.Items?[position]; });
        }

        public static implicit operator ComboBox(MDPicker self) => self.InternalComboBox;
    }
}