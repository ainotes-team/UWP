using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Helpers;
using Helpers.Controls;
using Helpers.Extensions;

namespace MaterialComponents {
    public struct MDPickerGridEntry {
        public object Value;
        public string Icon;
        public Action<object> SelectedCallback;

        public MDPickerGridEntry(object value, string icon, Action<object> selectedCallback) {
            Value = value;
            Icon = icon;
            SelectedCallback = selectedCallback;
        }
    }
    
    public partial class MDPickerGrid {
        protected const int SwatchSize = 26;
        
        protected readonly Color HighlightColor = Colors.LightBlue;

        protected readonly int OptionRowCount;
        protected readonly int OptionColumnCount;
        protected readonly List<MDPickerGridEntry> Options;

        private readonly Dictionary<MDPickerGridEntry, CustomFrame> _optionFrames = new Dictionary<MDPickerGridEntry, CustomFrame>();

        private void OnOptionTouch(object sender, WTouchEventArgs args) {
            if (args.ActionType != WTouchAction.Pressed) return;
            var frame = (CustomFrame) sender;

            foreach (var c in Children) {
                if (!(c is CustomFrame f)) continue;
                f.Background = Background;
            }
            frame.Background = HighlightColor.ToBrush();

            var option = _optionFrames.ReverseLookup(frame);
            option.SelectedCallback?.Invoke(option.Value);
            
        }
        
        public MDPickerGrid(int optionRowCount, int optionColumnCount, List<MDPickerGridEntry> options) {
            OptionRowCount = optionRowCount;
            OptionColumnCount = optionColumnCount;
            Options = options;
            
            InitializeComponent();

            for (var i = 0; i < OptionRowCount; i++) {
                RowDefinitions.Add(new RowDefinition {Height = new GridLength(SwatchSize)});
            }

            for (var i = 0; i < OptionColumnCount; i++) {
                ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(SwatchSize)});
            }
            
            var top = 0;
            var left = 0;
            foreach (var option in Options) {
                var icon = option.Icon;
                var frame = new CustomFrame {
                    Width = SwatchSize,
                    Height = SwatchSize,
                    BorderBrush = Theming.CurrentTheme.CardBorder,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(3),
                    Margin = new Thickness(0),
                    Content = new Image {
                        Source = new BitmapImage(new Uri(icon)),
                    },
                    CornerRadius = new CornerRadius(4),
                };

                frame.Touch += OnOptionTouch;
                Children.Add(frame, top, left);
                _optionFrames.Add(option, frame);

                if (left < ColumnDefinitions.Count - 1) {
                    left += 1;
                } else {
                    RowDefinitions.Add(new RowDefinition {Height = new GridLength(SwatchSize)});
                    top += 1;
                    left = 0;
                }
            }

            Height = RowDefinitions.Count * (SwatchSize + RowSpacing) - RowSpacing;
        }

        protected void SetSelectedValue(object value) {
            foreach (var (option, frame) in _optionFrames) {
                frame.Background = option.Value == value ? HighlightColor.ToBrush() : Background;
            }
        }
    }
}
