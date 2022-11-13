using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Web.Http;
using AINotes.Controls.Pages;
using Helpers;
using MaterialComponents;

namespace AINotes.Controls.Sidebar.Content {
    public class SidebarBrowserView : Frame, ISidebarView {
        private const string DefaultSource = "https://www.google.de";

        private MDEntry _searchBar;
        private MDButton _searchButton;
        private WebView _webView;

        private static readonly MDToolbarItem ReloadButton = new MDToolbarItem {
            ImageSource = new BitmapImage(new Uri(Icon.Reset)),
        };

        public IEnumerable<MDToolbarItem> ExtraButtons { get; } = new[] {
            ReloadButton
        };

        private bool _isFirstOverride = true;

        protected override Size ArrangeOverride(Size finalSize) {
            // ReSharper disable once InvertIf
            if (_isFirstOverride) {
                Content = new RelativePanel();

                ((RelativePanel) Content).Children.Add(_searchBar = new MDEntry {
                    Text = DefaultSource,
                    TextWrapping = TextWrapping.NoWrap,
                    MaxWidth = CustomContentPage.SidebarWidth - 130,
                });
                ((RelativePanel) Content).Children.Add(_searchButton = new MDButton {
                    Text = "Go",
                });
                ((RelativePanel) Content).Children.Add(_webView = new WebView());

                RelativePanel.SetAlignLeftWithPanel(_searchBar, true);
                RelativePanel.SetLeftOf(_searchBar, _searchButton);
                RelativePanel.SetAlignRightWithPanel(_searchButton, true);

                RelativePanel.SetAlignLeftWith(_webView, _searchBar);
                RelativePanel.SetAlignRightWith(_webView, _searchButton);
                RelativePanel.SetBelow(_webView, _searchBar);
                RelativePanel.SetAlignBottomWithPanel(_webView, true);

                _searchButton.Click += OnSearchButtonClick;
                ReloadButton.Pressed += OnReloadButtonPressed;
                _webView.NavigationStarting += OnNavigationStarting;

                NavigateTo(new Uri(DefaultSource));
                _isFirstOverride = false;
            }

            return base.ArrangeOverride(finalSize);
        }

        private void OnSearchButtonClick(object sender, RoutedEventArgs args) {
            Uri targetUri;
            try {
                targetUri = new Uri(_searchBar.Text);
            } catch (UriFormatException) {
                Logger.Log("[SidebarBrowserView]", "Invalid Uri", logLevel: LogLevel.Warning);
                App.Page.Notifications.Add(new MDNotification("Invalid URI"));
                return;
            }
            NavigateTo(targetUri);
        }

        private void OnReloadButtonPressed(object sender, EventArgs args) {
            NavigateTo(_webView.Source);
        }

        private void OnNavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args) {
            _webView.NavigationStarting -= OnNavigationStarting;
            args.Cancel = true;
            NavigateTo(args.Uri);
        }

        private void NavigateTo(Uri uri) {
            var msg = new HttpRequestMessage(HttpMethod.Get, uri);
            msg.Headers.Add("User-Agent", "Mozilla/5.0 (Linux; Android 7.0; SM-G930V Build/NRD90M) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.125 Mobile Safari/537.36");
            _webView.NavigateWithHttpRequestMessage(msg);

            _searchBar.Text = uri.OriginalString;

            _webView.NavigationStarting += OnNavigationStarting;
        }
    }
}