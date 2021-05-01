using System.Collections.Generic;
using System.Timers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Helpers;
using Helpers.Essentials;
using Helpers.Extensions;

namespace MaterialComponents {
    [ContentProperty(Name = nameof(Children))]
    public sealed partial class MDChipGroup {
        public static readonly DependencyProperty ChildrenProperty = DependencyProperty.Register(
            nameof(Children), 
            typeof(UIElementCollection),
            typeof(MDChipGroup),
            PropertyMetadata.Create(new List<UIElement>(), OnChildCollectionChanged)
        );

        private static void OnChildCollectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            Logger.Log("[MDChipGroup]", "OnChildCollectionChanged", e.OldValue, "=>", e.NewValue);
        }

        public UIElementCollection Children {
            get => (UIElementCollection) GetValue(ChildrenProperty);
            private set => SetValue(ChildrenProperty, value);
        }

        public MDChipGroup() {
            InitializeComponent();
            
            Children = ChildHost.Children;
                
            var defaultBrush = ColorCreator.FromHex("#E0E0E0").ToBrush();
            var hoverBrush = Colors.DarkGray.ToBrush();
            var pressedBrush = Colors.DimGray.ToBrush();

            // left
            void ScrollLeft() {
                MainThread.BeginInvokeOnMainThread(() => {
                    ChildScroller.ScrollToHorizontalOffset((ChildScroller.HorizontalOffset - 25).Clamp(0, double.MaxValue));
                });
            }

            var leftTimer = new Timer {
                Interval = 100,
                Enabled = false,
                AutoReset = true,
            };
            var leftHover = false;
            
            leftTimer.Elapsed += (s, e) => ScrollLeft();
            ScrollLeftNob.PointerPressed += (sender, args) => {
                leftTimer.Enabled = true;
                ScrollLeft();
                ScrollLeftNob.Background = pressedBrush;
            };
            ScrollLeftNob.PointerEntered += (sender, args) => {
                leftHover = true;
                ScrollLeftNob.Background = hoverBrush;
            };
            ScrollLeftNob.PointerExited += (sender, args) => {
                leftHover = false;
                leftTimer.Enabled = false;
                ScrollLeftNob.Background = defaultBrush;
            };
            ScrollLeftNob.PointerReleased += (sender, args) => {
                leftTimer.Enabled = false;
                ScrollLeftNob.Background = leftHover ? hoverBrush : defaultBrush;
            };
            
            // right
            var rightTimer = new Timer {
                Interval = 100,
                Enabled = false,
                AutoReset = false,
            };
            var rightHover = false;
            
            async void ScrollRight() {
                await MainThread.InvokeOnMainThreadAsync(() => {
                    ChildScroller.ScrollToHorizontalOffset((ChildScroller.HorizontalOffset + 25).Clamp(0, ChildScroller.ScrollableWidth));
                });
                
                rightTimer.Start();
            }

            rightTimer.Elapsed += (s, e) => ScrollRight();
            ScrollRightNob.PointerPressed += (sender, args) => {
                rightTimer.Start();
                ScrollRight();
                ScrollRightNob.Background = pressedBrush;
            };
            ScrollRightNob.PointerEntered += (sender, args) => {
                rightHover = true;
                ScrollRightNob.Background = hoverBrush;
            };
            ScrollRightNob.PointerExited += (sender, args) => {
                rightHover = false;
                ScrollRightNob.Background = defaultBrush;
            };
            ScrollRightNob.PointerReleased += (sender, args) => {
                rightTimer.Stop();
                ScrollRightNob.Background = rightHover ? hoverBrush : defaultBrush;
            };
        }
    }
}