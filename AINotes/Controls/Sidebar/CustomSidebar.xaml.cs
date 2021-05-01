using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using AINotes.Controls.Pages;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Helpers;
using Helpers.Extensions;
using Helpers.Lists;
using MaterialComponents;
using ColumnDefinition = Windows.UI.Xaml.Controls.ColumnDefinition;

namespace AINotes.Controls.Sidebar {
    public enum SidebarState {
        Disabled,
        Hidden,
        Collapsed,
        Opened
    }

    public enum SidebarPosition {
        Left,
        Right
    }

    public partial class CustomSidebar {
        // state
        private MDToolbarItem _selectedToolbarItem;

        private ColumnDefinition _contentColumnDefinition;
        
        private SidebarPosition _position;
        public SidebarPosition Position {
            get => _position;
            set {
                Logger.Log("[CustomSidebar]", "Position =", value);
                
                _position = value;
                UpdateSources();
                
                switch (value) {
                    case SidebarPosition.Left:
                        ColumnDefinitions.Clear();
                        _contentColumnDefinition = new ColumnDefinition {
                            Width = new GridLength(CustomContentPage.SidebarWidth - CustomContentPage.SidebarWidthCollapsed)
                        };

                        switch (State) {
                            case SidebarState.Disabled:
                                _contentColumnDefinition.Width = new GridLength(0);
                                break;
                            case SidebarState.Hidden:
                                _contentColumnDefinition.Width = new GridLength(0);
                                break;
                            case SidebarState.Collapsed:
                                _contentColumnDefinition.Width = new GridLength(0);
                                break;
                            case SidebarState.Opened:
                                _contentColumnDefinition.Width = new GridLength(CustomContentPage.SidebarWidth - CustomContentPage.SidebarWidthCollapsed);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        
                        ColumnDefinitions.AddRange(new [] {
                            _contentColumnDefinition,
                            new ColumnDefinition {
                                Width = new GridLength(CustomContentPage.SidebarWidthCollapsed)
                            },
                        });

                        SetRow(SidebarItemGrid, 0);
                        SetColumn(SidebarItemGrid, 1);
                        
                        SetRow(Content, 0);
                        SetRowSpan(Content, 2);
                        SetColumn(Content, 0);
                        
                        SetRow(ChangeStateButton, 1);
                        SetColumn(ChangeStateButton, 1);
                        
                        BorderThickness = new Thickness(0, 0, 1, 0);
                        break;
                    case SidebarPosition.Right:
                        ColumnDefinitions.Clear();

                        _contentColumnDefinition = null;
                        ColumnDefinitions.AddRange(new [] {
                            new ColumnDefinition {
                                Width = new GridLength(CustomContentPage.SidebarWidthCollapsed)
                            },
                            new ColumnDefinition {
                                Width = new GridLength(CustomContentPage.SidebarWidth - CustomContentPage.SidebarWidthCollapsed)
                            },
                        });
                        
                        SetRow(SidebarItemGrid, 0);
                        SetColumn(SidebarItemGrid, 0);
                        
                        SetRow(Content, 0);
                        SetRowSpan(Content, 2);
                        SetColumn(Content, 1);
                        
                        SetRow(ChangeStateButton, 1);
                        SetColumn(ChangeStateButton, 0);
                        
                        BorderThickness = new Thickness(1, 0, 0, 0);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }
        }
        
        private SidebarState _state = SidebarState.Collapsed;
        public SidebarState State {
            get => _state;
            set {
                Logger.Log("[CustomSidebar]", "State =", value);
                
                UpdateSources();
                _state = value;

                ColumnDefinition targetDefinition;
                switch (Position) {
                    case SidebarPosition.Left:
                        targetDefinition = App.Page.LeftSidebarDefinition;
                        break;
                    case SidebarPosition.Right:
                        targetDefinition = App.Page.RightSidebarDefinition;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                switch (value) {
                    case SidebarState.Disabled:
                        targetDefinition.Width = new GridLength(0);
                        if (_contentColumnDefinition != null) _contentColumnDefinition.Width = new GridLength(0);
                        break;
                    case SidebarState.Hidden:
                        targetDefinition.Width = new GridLength(0);
                        if (_contentColumnDefinition != null) _contentColumnDefinition.Width = new GridLength(0);
                        break;
                    case SidebarState.Collapsed:
                        targetDefinition.Width = new GridLength(CustomContentPage.SidebarWidthCollapsed + 1);
                        if (_contentColumnDefinition != null) _contentColumnDefinition.Width = new GridLength(0);
                        break;
                    case SidebarState.Opened:
                        targetDefinition.Width = new GridLength(CustomContentPage.SidebarWidth + 1);
                        if (_contentColumnDefinition != null) _contentColumnDefinition.Width = new GridLength(CustomContentPage.SidebarWidth - CustomContentPage.SidebarWidthCollapsed);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }
        }

        public void SetContent(UIElement content) {
            SidebarContent.Content = content;
        }

        public void SetTitle(string title) {
            SidebarCaption.Text = title;
        }

        public void SendItemPress(int index) {
            ((MDToolbarItem) SidebarItems.Children[index]).SendPress();
        }

        public ObservableList<MDToolbarItem> Items { get; set; } = new ObservableList<MDToolbarItem>();
        
        public CustomSidebar() {
            InitializeComponent();
            
            Items.CollectionChanged += OnItemCollectionChanged;
            
            Padding = new Thickness(0);
            Margin = new Thickness(0);
            ColumnSpacing = 0;
            RowSpacing = 0;
            
            Background = Configuration.Theme.Background;
            BorderBrush = ColorCreator.FromHex("#EDEDEF").ToBrush();
            
            RowDefinitions.AddRange(new[] {
                // items
                new RowDefinition {
                    Height = new GridLength(1, GridUnitType.Star)
                },

                // collapse / show button placeholder
                new RowDefinition {
                    Height = new GridLength(48)
                }
            });

            SidebarItems.Background = Configuration.Theme.Background;

            CloseButton.Released += OnCloseButtonReleased;
            ChangeStateButton.Released += OnChangeStateButtonReleased;

            SidebarItemGrid.Background = Configuration.Theme.Background;
            SidebarContent.Background = Configuration.Theme.Background;
            
            // add children
            RelativePanel.SetAlignTopWithPanel(TitlebarContainer, true);
            RelativePanel.SetAlignRightWithPanel(TitlebarContainer, true);
            RelativePanel.SetAlignLeftWithPanel(TitlebarContainer, true);
            
            RelativePanel.SetBelow(SidebarContent, TitlebarContainer);
            RelativePanel.SetAlignRightWithPanel(SidebarContent, true);
            RelativePanel.SetAlignLeftWithPanel(SidebarContent, true);
            RelativePanel.SetAbove(SidebarContent, BottomNotificationPanel);
            
            RelativePanel.SetAlignRightWithPanel(BottomNotificationPanel, true);
            RelativePanel.SetAlignLeftWithPanel(BottomNotificationPanel, true);
            RelativePanel.SetAlignBottomWithPanel(BottomNotificationPanel, true);
            
            Position = _position;
        }
        
        private void OnCloseButtonReleased(object s, EventArgs e) {
            State = SidebarState.Collapsed;
            _selectedToolbarItem.IsSelected = false;
            _selectedToolbarItem = null;
        }

        private void OnChangeStateButtonReleased(object s, EventArgs e) {
            switch (State) {
                case SidebarState.Disabled:
                    throw new ArgumentOutOfRangeException();
                case SidebarState.Hidden:
                    State = SidebarState.Collapsed;
                    if (_selectedToolbarItem == null) return;
                    _selectedToolbarItem.IsSelected = false;
                    _selectedToolbarItem = null;
                    ChangeStateButton.ImageSource = new BitmapImage(new Uri(Icon.ArrowRight));
                    break;
                case SidebarState.Collapsed:
                case SidebarState.Opened:
                    State = SidebarState.Hidden;
                    if (_selectedToolbarItem == null) return;
                    _selectedToolbarItem.IsSelected = false;
                    _selectedToolbarItem = null;
                    ChangeStateButton.ImageSource = new BitmapImage(new Uri(Icon.ArrowLeft));
                    break;
            }
        }

        private void OnItemCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            Logger.Log("OnItemCollectionChanged", e.Action);
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    foreach (var itm in e.NewItems) {
                        SidebarItems.Children.Add((MDToolbarItem) itm);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var itm in e.OldItems) {
                        SidebarItems.Children.Remove((MDToolbarItem) itm);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    SidebarItems.Children.Clear();
                    break;
            }
        }

        private void UpdateSources() {
            switch (Position) {
                case SidebarPosition.Left:
                    switch (State) {
                        case SidebarState.Hidden:
                            ChangeStateButton.ImageSource = new BitmapImage(new Uri(Icon.ArrowRight));
                            break;
                        case SidebarState.Collapsed:
                            ChangeStateButton.ImageSource = new BitmapImage(new Uri(Icon.ArrowLeft));
                            break;
                        case SidebarState.Opened:
                            ChangeStateButton.ImageSource = new BitmapImage(new Uri(Icon.ArrowLeft));
                            break;
                    }
                    break;
                case SidebarPosition.Right:
                    switch (State) {
                        case SidebarState.Hidden:
                            ChangeStateButton.ImageSource = new BitmapImage(new Uri(Icon.ArrowLeft));
                            break;
                        case SidebarState.Collapsed:
                            ChangeStateButton.ImageSource = new BitmapImage(new Uri(Icon.ArrowRight));
                            break;
                        case SidebarState.Opened:
                            ChangeStateButton.ImageSource = new BitmapImage(new Uri(Icon.ArrowRight));
                            break;
                    }
                    break;
            }
        }

        public void ShowExtraButtons(IEnumerable<MDToolbarItem> buttons) {
            ClearTitlebar();
            if (buttons == null) return;
            var items = buttons.ToList();
            Logger.Log("ShowExtraButtons", items.ToFString() ?? "-");
            ClearTitlebar();
            foreach (var button in items) {
                TitlebarButtonsContainer.Children.Add(button);
            }
        }

        public void ClearTitlebar() {
            foreach (var tbi in TitlebarButtonsContainer.Children.Where(itm => itm is MDToolbarItem && itm != CloseButton)) {
                TitlebarButtonsContainer.Children.Remove(tbi);
            }
        }

        public readonly Dictionary<MDToolbarItem, Action<MDToolbarItem>> Callbacks = new Dictionary<MDToolbarItem, Action<MDToolbarItem>>();

        private void SidebarItemCallback(object obj, EventArgs e) {
            var item = (MDToolbarItem) obj;
            
            var selectedTBI = _selectedToolbarItem;
            if (selectedTBI != null) {
                selectedTBI.IsSelected = false;
            }

            if (_selectedToolbarItem != null) _selectedToolbarItem.BorderBrush = Colors.Transparent.ToBrush();
            if (_selectedToolbarItem == item) {
                State = SidebarState.Collapsed;
                _selectedToolbarItem = null;
            } else {
                switch (State) {
                    case SidebarState.Disabled:
                    case SidebarState.Hidden:
                        Logger.Log("[CustomSidebar]", "Callback in State Hidden", logLevel: LogLevel.Warning);
                        break;
                    case SidebarState.Collapsed:
                        State = SidebarState.Opened;
                        break;
                }

                _selectedToolbarItem = item;
                Callbacks[item]?.Invoke(item);
            }
        }

        public void ClearItems() {
            SidebarItems.Children.Clear();
            Callbacks.Clear();
        }

        public void AddItem(string source, Action<MDToolbarItem> callback = null) {
            // Logger.Log("[CustomSidebar]", $"-> AddItem({source}, ...)", logLevel: LogLevel.Verbose);
            var toolbarItem = new MDToolbarItem {
                ImageSource = new BitmapImage(new Uri(source)),
                Selectable = true,
                Padding = new Thickness(6, 0, 6, 0),
            };
            toolbarItem.Released += SidebarItemCallback;
            Callbacks.Add(toolbarItem, callback);
            SidebarItems.Children.Add(toolbarItem);
            // Logger.Log("[CustomSidebar]", "<- AddItem", logLevel: LogLevel.Verbose);
        }
    }
}