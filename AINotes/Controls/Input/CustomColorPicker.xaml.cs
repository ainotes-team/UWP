using System;
using System.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AINotes.Controls.Popups;
using AINotes.Helpers.Imaging;
using Helpers;
using Helpers.Controls;
using Helpers.Extensions;

namespace AINotes.Controls.Input {
    public partial class CustomColorPicker {
        public const int SwatchSize = 26;
        
        public ArrayList DefaultColors = new ArrayList {
            "#FFC114",
            "#F6630D",
            "#FF0066",
            "#E71225",
            "#5B2D90",
            "#AB008B",
            "#CC0066",
            "#004F8B",
            "#00A0D7",
            "#33CCFF",
            "#008C3A",
            "#66CC00",
            "#000000",
            "#333333",
            "#849398",
            "#FFFF00",
            "#7FFF00",
            "#FFFFFF",
        };

        public event Action<Color> ColorSelected;

        public CustomColorPicker(string selectHexColor = null, bool showPlus = false) {
            ColumnSpacing = RowSpacing = 6;

            RowDefinitions.AddRange(new [] {
                new RowDefinition {Height = new GridLength(SwatchSize)}
            });
            
            ColumnDefinitions.AddRange(new [] {
                new ColumnDefinition {Width = new GridLength(SwatchSize)},
                new ColumnDefinition {Width = new GridLength(SwatchSize)},
                new ColumnDefinition {Width = new GridLength(SwatchSize)},
                new ColumnDefinition {Width = new GridLength(SwatchSize)},
                new ColumnDefinition {Width = new GridLength(SwatchSize)}
            });

            var top = 0;
            var left = 0;
            foreach (string hexColor in DefaultColors) {
                var color = ColorCreator.FromHex(hexColor);
                var frame = new CustomFrame {
                    Width = Height = SwatchSize,
                    Background = color.ToBrush(),
                    BorderThickness = new Thickness(1),
                    BorderBrush = Configuration.Theme.CardBorder,
                    CornerRadius = new CornerRadius(4),
                };

                void OnFrameTouch(object o, WTouchEventArgs args) {
                    if (args.ActionType != WTouchAction.Pressed) return;
                    ColorSelected?.Invoke(color);
                }

                frame.Touch += OnFrameTouch;
                if (hexColor == selectHexColor) {
                    frame.BorderThickness = new Thickness(3);
                }
                
                this.AddChild(frame, top, left);

                if (left < ColumnDefinitions.Count - 1) {
                    left += 1;
                } else {
                    RowDefinitions.Add(new RowDefinition {Height = new GridLength(SwatchSize)});
                    top += 1;
                    left = 0;
                }
            }

            if (showPlus) {
                var frame = new CustomFrame {
                    Width = Height = SwatchSize,
                    Background = Background,
                    BorderBrush = Configuration.Theme.CardBorder,
                    Content = new Image {
                        Source = ImageSourceHelper.FromName(Icon.Plus),
                    }
                };
                frame.Touch += OnPlusFrameTouch;
                this.AddChild(frame, top, left);
            }

            Height = RowDefinitions.Count * (SwatchSize + RowSpacing) - RowSpacing;
        }

        private void OnPlusFrameTouch(object o, WTouchEventArgs args) {
            // TODO: Add Color Code
            if (args.ActionType != WTouchAction.Pressed) return;
            new MDContentPopup("Add Color", new ColorPicker()).Show();
        }
    }
}