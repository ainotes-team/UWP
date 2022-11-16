using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using AINotes.Controls.Sidebar;
using AINotes.Helpers.PreferenceHelpers;
using Helpers;

namespace AINotes.Controls.Pages {
    public sealed partial class CustomContentPage {
        public const int SidebarWidth = 400;
        public const int SidebarWidthCollapsed = 48;
        
        public IList<UIElement> Notifications => NotificationContainer.Children;

        private CustomPageContent _currentContent;
        public new CustomPageContent Content {
            get => _currentContent;
            private set {
                ContentChanging?.Invoke(_currentContent, value);
                _currentContent = value;
                ContentContainer.Child = value;
            }
        }

        public UIElementCollection PrimaryToolbarChildren => Toolbar.PrimaryToolbarChildren;
        public UIElementCollection SecondaryToolbarChildren => Toolbar.SecondaryToolbarChildren;
        
        public string Title {
            set => Toolbar.Title = value;
        }

        public Action OnBackPressed {
            set => Toolbar.BackCallback = value;
        }

        public void GoBack() => Toolbar.BackCallback?.Invoke();

        public event Action<CustomPageContent, CustomPageContent> ContentChanging;
        
        public CustomContentPage() {
            InitializeComponent();

            // theme subscription
            Background = Configuration.Theme.Background;
            Preferences.ThemeChanged += () => Background = Configuration.Theme.Background;
            
            // global touch events
            App.Current.Touch += OnGlobalTouch;
            
            // global core pointer events
            Window.Current.CoreWindow.PointerPressed += OnCoreWindowPointerPressed;
            
            // populate sidebars
            PopulateLeftSidebar();
            PopulateRightSidebar();
        }

        private void OnCoreWindowPointerPressed(CoreWindow sender, PointerEventArgs args) {
            // mouse back button
            if (args.CurrentPoint.Properties.IsXButton1Pressed) {
                App.Page.GoBack();
            }
        }

        private void OnGlobalTouch(object s, WTouchEventArgs e) {
            if (!CustomDropdown.IsOpen()) return;
            if (!e.InContact) return;
            if (e.Id == -10 /* touches with id -10 are holding events not natively supported by the SKTouchEventArgs */) return;
            if (e.ActionType != WTouchAction.Pressed) return;
            if (e.Handled) return;
            if (CustomDropdown.DropdownProtection) {
                Logger.Log("[CustomContentPage]", "OnGlobalTouch: DropdownProtection");
                CustomDropdown.DropdownProtection = false;
                return;
            }

            Logger.Log("TODO: Touch =?> CloseDropdown()");
            // CustomDropdown.CloseDropdown();
        }

        public void PopulateLeftSidebar() {
            Logger.Log("[CustomContentPage]", "-> PopulateLeftSidebar");
            LeftSidebar.ClearItems();

            var itemAddActionsFromPreference = ((List<SidebarItemModel>) Preferences.LeftSidebarItems).Where(itm => itm.IsEnabled).Select<SidebarItemModel, Action<CustomSidebar>>(model => model.AddToSidebar);
            foreach (var addAction in itemAddActionsFromPreference) {
                addAction.Invoke(LeftSidebar);
            }
            
            Logger.Log("[CustomContentPage]", "<- PopulateLeftSidebar");
        }
        
        public void PopulateRightSidebar() {
            Logger.Log("[CustomContentPage]", "-> PopulateRightSidebar");
            RightSidebar.ClearItems();

            var itemAddActionsFromPreference = ((List<SidebarItemModel>) Preferences.RightSidebarItems).Where(itm => itm.IsEnabled).Select<SidebarItemModel, Action<CustomSidebar>>(model => model.AddToSidebar);
            foreach (var addAction in itemAddActionsFromPreference) {
                addAction.Invoke(RightSidebar);
            }
            
            Logger.Log("[CustomContentPage]", "<- PopulateRightSidebar");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            Logger.Log("[CustomContentPage]", "OnNavigatedTo", e.Parameter, e.Content, e.Uri, e.NavigationMode, e.NavigationTransitionInfo);
            base.OnNavigatedTo(e);
        }

        private bool _loading;
        /// <summary>Handles Loading and Unloading of <see cref="T:AINotes.Controls.Pages.CustomPageContent"></see>. Use this to navigate between pages.</summary>
        /// <param name="content">The page content instance to display.</param>
        public void Load(CustomPageContent content) {
            Logger.Log("[CustomContentPage]", "-> Load");
            if (_loading) throw new ThreadStateException("Loading");
            _loading = true;
            
            Content?.OnUnload();
            Logger.Log("[CustomContentPage]", "Load -> Unloaded");
            Content = content;
            Logger.Log("[CustomContentPage]", "Load -> Content set");
            Content.OnLoad();
            Logger.Log("[CustomContentPage]", "Load -> Loaded");

            _loading = false;
            Logger.Log("[CustomContentPage]", "<- Load");
        }
    }
}
