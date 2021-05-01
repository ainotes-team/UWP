using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Helpers.Essentials;

namespace MaterialComponents {
    public partial class MDNotification {
        public readonly MDLabel AdditionalInfoLabel;

        public string Title {
            get => TitleLabel.Text;
            set => TitleLabel.Text = value;
        }

        public string AdditionalInfo {
            get => AdditionalInfoLabel.Text;
            set => AdditionalInfoLabel.Text = value;
        }

        public bool RemoveOnAccept;
        public bool RemoveOnDismiss;

        public Action<MDNotification> AcceptAction;
        public Action<MDNotification> DismissAction;

        public MDNotification(string title, Action<MDNotification> acceptAction = null, Action<MDNotification> dismissAction = null, bool removeOnAccept = true, bool removeOnDismiss = true, string acceptButtonText = null, string dismissButtonText = null, string additionalInfo = "") {
            InitializeComponent();
            
            // params
            Title = title;
            AcceptAction = acceptAction;
            DismissAction = dismissAction;
            RemoveOnAccept = removeOnAccept;
            RemoveOnDismiss = removeOnDismiss;

            Background = Theming.CurrentTheme.Background;
            BorderBrush = Theming.CurrentTheme.CardBorder;

            // components
            AcceptButton.Text = acceptButtonText ?? "Apply";
            
            AcceptButton.Click += (_, __) => {
                AcceptAction?.Invoke(this);
                if (RemoveOnAccept) ((StackPanel) Parent).Children.Remove(this);
            };

            DismissButton.Text = dismissButtonText ?? "Dismiss";
            
            DismissButton.Click += (_, __) => {
                DismissAction?.Invoke(this);
                if (RemoveOnDismiss) ((StackPanel) Parent).Children.Remove(this);
            };
            
            if (dismissButtonText == null) ButtonLayout.Children.Remove(DismissButton);

            AdditionalInfoLabel = new MDLabel {
                Text = additionalInfo,
                Margin = new Thickness(0)
            };

            if (additionalInfo != null) {
                InfoLayout.Children.Add(AdditionalInfoLabel);
            }
        }

        public void Update(string additionalInfo, string acceptButtonText = null, string dismissButtonText = null, bool removeOnAccept = false, bool removeOnDismiss = false, Action<MDNotification> acceptAction = null, Action<MDNotification> dismissAction = null) {
            MainThread.BeginInvokeOnMainThread(() => {
                // additional info
                AdditionalInfoLabel.Text = additionalInfo;

                // accept button
                RemoveOnAccept = removeOnAccept;
                AcceptAction = acceptAction;
                if (acceptButtonText == null) {
                    ButtonLayout.Children.Remove(AcceptButton);
                } else {
                    if (AcceptButton.Parent == null) {
                        ButtonLayout.Children.Insert(0, AcceptButton);
                    }

                    AcceptButton.Text = acceptButtonText;
                }

                // dismiss button
                RemoveOnDismiss = removeOnDismiss;
                DismissAction = dismissAction;
                if (dismissButtonText == null) {
                    ButtonLayout.Children.Remove(DismissButton);
                } else {
                    if (DismissButton.Parent == null) {
                        ButtonLayout.Children.Add(DismissButton);
                    }

                    DismissButton.Text = dismissButtonText;
                }
            });
        }
    }
}