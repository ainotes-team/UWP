using System;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Helpers;
using Helpers.Essentials;
using MaterialComponents;

namespace AINotes.Controls.Popups {
    public class MDContentPopup : MDPopup {
        private readonly Frame _errorBar = new Frame();
        
        public MDContentPopup(string title, UIElement content, Action okCallback = null, bool closeWhenBackgroundIsClicked = true, bool cancelable = false, Action cancelCallback = null, bool closeOnOk = true, string okText = null, string cancelText = null, bool submitable=true, UIElement[] buttons = null) {
            CloseWhenBackgroundIsClicked = closeWhenBackgroundIsClicked;

            var buttonLayout = new StackPanel {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 10
            };

            if (cancelable) {
                var cancelButton = new MDButton {
                    Text = cancelText ?? ResourceHelper.GetString("cancel"),
                    ButtonStyle = MDButtonStyle.Secondary,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Command = () => {
                        cancelCallback?.Invoke();
                        CloseCurrentPopup();
                    }
                };
                buttonLayout.Children.Add(cancelButton);
            }

            if (buttons != null) {
                foreach (var button in buttons) {
                    buttonLayout.Children.Add(button);
                }
            }

            if (submitable) {
                var submitButton = new MDButton {
                    Text = okText ?? ResourceHelper.GetString("ok"),
                    ButtonStyle = MDButtonStyle.Primary,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Command = () => {
                        okCallback?.Invoke();
                        if (closeOnOk) {
                            CloseCurrentPopup();
                        }
                    }
                };

                buttonLayout.Children.Add(submitButton);
            }

            var contentStack = new StackPanel();

            contentStack.Children.Add(new MDLabel {Text = title});
            contentStack.Children.Add(new Frame {Height = 10});

            contentStack.Children.Add(content);

            contentStack.Children.Add(new Frame {Height = 10});
            contentStack.Children.Add(buttonLayout);
            
            contentStack.Children.Add(_errorBar);

            Content = new Frame {
                Background = Configuration.Theme.Background,
                Margin = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Content = contentStack
            };
        }

        public void ShowErrorMessage(string errorMessage) {
            var errorBox = new Frame {
                Background = new SolidColorBrush(Colors.Red),
                Margin = new Thickness(4),
                CornerRadius = new CornerRadius(4),
                Content = new StackPanel {
                    Orientation = Orientation.Horizontal,
                    Children = {
                        new MDLabel {
                            Text = errorMessage,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(6)
                        }
                    }
                }
            };

            _errorBar.Content = errorBox;
            
            ThreadPoolTimer.CreatePeriodicTimer(t => {
                MainThread.BeginInvokeOnMainThread(() => _errorBar.Content = null);
                t.Cancel();
            }, TimeSpan.FromSeconds(10));
        }
    }
}