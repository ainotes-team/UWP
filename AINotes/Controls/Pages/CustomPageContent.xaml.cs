using System;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AINotes.Controls.Sidebar;
using Helpers.Essentials;
using Helpers.Extensions;

namespace AINotes.Controls.Pages {
    public partial class CustomPageContent {
        public static readonly DependencyProperty DiscordDetailsProperty = DependencyProperty.Register(
            "DiscordDetails",
            typeof(string),
            typeof(CustomPageContent),
            new PropertyMetadata("")
        );
        
        public static readonly DependencyProperty DiscordDetailsStateProperty = DependencyProperty.Register(
            "DiscordDetailsState",
            typeof(string),
            typeof(CustomPageContent),
            new PropertyMetadata("")
        );
        
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            "Title",
            typeof(string),
            typeof(CustomPageContent),
            new PropertyMetadata("")
        );
        
        public string DiscordDetails {
            get => (string) GetValue(DiscordDetailsProperty);
            set {
                SetValue(DiscordDetailsProperty, value);
                UpdateDiscordDetails();
            }
        }

        public string DiscordDetailsState {
            get => (string) GetValue(DiscordDetailsStateProperty);
            set {
                SetValue(DiscordDetailsStateProperty, value);
                UpdateDiscordDetails();
            }
        }

        public string Title {
            get => (string) GetValue(TitleProperty);
            // ReSharper disable once UnusedMember.Global
            set {
                SetValue(TitleProperty, value);
                UpdateTitle();
            }
        }

        public UIElementCollection MainToolbarItems {
            get => App.Page.PrimaryToolbarChildren;
            set {
                App.Page.PrimaryToolbarChildren.Clear();
                App.Page.PrimaryToolbarChildren.AddRange(value);
            }
        }
        
        public UIElementCollection SecondaryToolbarItems {
            get => App.Page.SecondaryToolbarChildren;
            set {
                App.Page.SecondaryToolbarChildren.Clear();
                App.Page.SecondaryToolbarChildren.AddRange(value);
            }
        }

        private bool _isActive;

        private bool _leftSidebarEnabled;
        private bool _rightSidebarEnabled;

        private SidebarState _rightSidebarState;
        private SidebarState _leftSidebarState;
        public virtual void OnLoad() {
            _isActive = true;
            (_leftSidebarEnabled, _rightSidebarEnabled) = ((Dictionary<Type, (bool, bool)>) Preferences.SidebarStates)[GetType()];
            
            if (!_leftSidebarEnabled) {
                _leftSidebarState = App.Page.LeftSidebar.State;
                App.Page.LeftSidebar.State = SidebarState.Disabled;
            }
            
            // ReSharper disable once InvertIf
            if (!_rightSidebarEnabled) {
                _rightSidebarState = App.Page.RightSidebar.State;
                App.Page.RightSidebar.State = SidebarState.Disabled;
            }

            UpdateDiscordDetails();
            UpdateTitle();
        }
        
        public virtual void OnUnload() {
            _isActive = false;
            if (!_leftSidebarEnabled) {
                App.Page.LeftSidebar.State = _leftSidebarState;
            }
            
            if (!_rightSidebarEnabled) {
                App.Page.RightSidebar.State = _rightSidebarState;
            }
        }

        private async void UpdateDiscordDetails() {
            if (!_isActive) return;
            if (App.Connection == null) return;
            await App.SendToAppService(new ValueSet {{"setDiscordPresence", (DiscordDetails, DiscordDetailsState).Serialize()}});
        }

        private void UpdateTitle() {
            if (!_isActive) return;
            MainThread.BeginInvokeOnMainThread(() => {
                if (!_isActive) return;
                App.Page.Title = Title;
            });
        }
    }
}