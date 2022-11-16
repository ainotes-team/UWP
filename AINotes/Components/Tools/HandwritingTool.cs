using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using AINotes.Controls.Input;
using AINotes.Controls.Pages;
using AINotes.Helpers;
using AINotes.Helpers.Imaging;
using Helpers.Essentials;
using AINotes.Models;
using AINotes.Models.Enums;
using Helpers;
using Helpers.Controls;
using Helpers.Extensions;
using MaterialComponents;
using Newtonsoft.Json;

namespace AINotes.Components.Tools {
    public enum InkPenType {
        Normal,
        IgnorePressure,
        Marker
    }
    
    public class HandwritingTool : MDToolbarItem, ITool {
        public bool RequiresDrawingLayer => true;
        
        public static Dictionary<PenModel, MDToolbarItem> PenToolbarItems { get; } = new Dictionary<PenModel, MDToolbarItem>();
        public static List<(int, PenModel)> PenModels = new List<(int, PenModel)>();
        
        public static readonly Dictionary<InkPenType, string> PenIcons = new Dictionary<InkPenType, string> {
            { InkPenType.Normal, Icon.Pencil },
            { InkPenType.IgnorePressure, Icon.Pen },
            { InkPenType.Marker, Icon.ChiselTipMarker},
        };

        public HandwritingTool() {
            ImageSource = ImageSourceHelper.FromName(Icon.Pen);
            Selectable = true;
            Deselectable = false;
            
            LoadPenModels();
        }

        public void Select() {
            Logger.Log("[HandwritingTool]", "-> Select");
            CustomDropdown.CloseDropdown();
            LoadExtraToolbarItems();
            App.EditorScreen.SetInkDrawingMode(InkCanvasMode.Draw);
            SendPress();
            Logger.Log("[HandwritingTool]", "<- Select");
        }

        public void Deselect() {
            PenToolbarItems.Clear();
            IsSelected = false;
        }

        public void SubscribeToPressedEvents(EventHandler<EventArgs> handler) => Pressed += handler;
        
        private static void LoadPenModels() {
            Logger.Log("[HandwritingTool]", "-> LoadPenModels", logLevel: LogLevel.Debug);
            var penJson = SavedStatePreferenceHelper.Get("saved_pen_models", "[]");
            Logger.Log("[HandwritingTool]", "LoadPenModels: PenJson:", penJson);
            
            var settings = new JsonSerializerSettings {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };
            settings.Converters.Add(new SizeJsonConverter());
            settings.Converters.Add(new ColorJsonConverter());
            
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            try {
                if (penJson.StartsWith("{")) {
                    // legacy dict support
                    var deserialized = JsonConvert.DeserializeObject<Dictionary<int, PenModel>>(penJson)?.Select(kv => (kv.Key, kv.Value)) ?? new List<(int, PenModel)>();
                    PenModels = deserialized.ToList();
                } else {
                    var deserialized = JsonConvert.DeserializeObject<List<(int, PenModel)>>(penJson, settings) ?? new List<(int, PenModel)>();
                    PenModels = deserialized.ToList();
                }
            } catch (Exception ex) {
                Logger.Log("[HandwritingTool]", "LoadPenModels - Exception:", ex.ToString(), logLevel: LogLevel.Error);
            }

            // load into inkCanvas
            var lastUsedPenIndex = SavedStatePreferenceHelper.Get("lastUsedPenIndex", 0);
            if (PenModels.Count == 0) return;
            Logger.Log("[HandwritingTool]", "LoadPenModels: PenModels.Count != 0", lastUsedPenIndex);
            
            PenModel selectPenModel;
            if (PenModels.Count > lastUsedPenIndex && lastUsedPenIndex > 0) {
                selectPenModel = PenModels[lastUsedPenIndex].Item2;
            } else {
                selectPenModel = PenModels[0].Item2;
            }

            ApplyPenModel(selectPenModel);

            Logger.Log("[HandwritingTool]", "<- LoadPenModels", PenModels.Count, logLevel: LogLevel.Debug);
        }

        private static void ApplyPenModel(PenModel penModel) {
            PenTipShape penTip;
            bool highlight;
            bool ignorePressure;
            bool ignoreTilt;
            switch (penModel.PenType) {
                case InkPenType.Normal:
                    penTip = PenTipShape.Circle;
                    highlight = false;
                    ignorePressure = ignoreTilt = false;

                    break;
                case InkPenType.IgnorePressure:
                    penTip = PenTipShape.Circle;
                    highlight = false;
                    ignorePressure = ignoreTilt = true;
                    break;
                case InkPenType.Marker:
                    penTip = PenTipShape.Rectangle;
                    highlight = true;
                    ignorePressure = ignoreTilt = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(penModel.PenType));
            }

            var idx = PenModels.FirstOrDefault(idxPenModel => idxPenModel.Item2 == penModel).Item1;
            SavedStatePreferenceHelper.Set("lastUsedPenIndex", idx);
            App.EditorScreen?.SetInkProperties(penModel.Color, penModel.Size, penTip, highlight, ignorePressure, ignoreTilt);
        }

        public static void LoadExtraToolbarItems() {
            Logger.Log("[HandwritingTool]", "-> LoadExtraToolbarItems", logLevel: LogLevel.Debug);
            
            // add pens
            Logger.Log("PenModels:", PenModels?.Count);
            foreach (var (idx, pen) in PenModels ?? new List<(int, PenModel)>()) {
                AddPen(pen, idx);
            }
            var lastUsedPenIndex = SavedStatePreferenceHelper.Get("lastUsedPenIndex", 0);
            Logger.Log("[HandwritingTool]", "LoadExtraToolbarItems: lastUsedPenIndex =", lastUsedPenIndex, logLevel: LogLevel.Debug);
            if (PenToolbarItems.Count > 0) {
                // select last used or first
                if (App.Page.SecondaryToolbarChildren.Count > lastUsedPenIndex && lastUsedPenIndex > 0) {
                    ((MDToolbarItem)App.Page.SecondaryToolbarChildren[lastUsedPenIndex]).SendPress();
                } else {
                    ((MDToolbarItem)App.Page.SecondaryToolbarChildren[0]).SendPress();
                }
            } else {
                // load default pens if no pens are loaded
                AddPen(new PenModel(Colors.Black, new Size(1, 1), InkPenType.IgnorePressure), 0);
                AddPen(new PenModel(Colors.Red, new Size(3, 3), InkPenType.Normal), 1);
                AddPen(new PenModel(Colors.Green, new Size(3, 3), InkPenType.Normal), 2);
                AddPen(new PenModel(Colors.RoyalBlue, new Size(5, 5), InkPenType.IgnorePressure), 3);
                AddPen(new PenModel(Colors.Yellow, new Size(5, 5), InkPenType.Marker), 4);
                AddPen(new PenModel(Colors.Chartreuse, new Size(5, 5), InkPenType.Marker), 5);
                
                SavePens();
            }
            
            // add + toolbar item
            App.EditorScreen.AddExtraToolbarItem(new MDToolbarItem(Icon.Add, OnAddPenItemPressed) {
                Selectable = false
            });
            
            Logger.Log("[HandwritingTool]", "<- LoadExtraToolbarItems", logLevel: LogLevel.Debug);
        }

        private static void OnAddPenItemPressed(object o, EventArgs eventArgs) {
            var penModel = new PenModel(Colors.Black, new Size(3, 3), InkPenType.Normal);
            MainThread.BeginInvokeOnMainThread(() => {
                var newToolbarItem = AddPen(penModel);
                ShowPenSettingsDropdown(newToolbarItem, penModel.Size, penModel.PenType, penModel.Color);
                // penToolbarItem.SendPress();
                // SavePens();
            });
        }

        private static void UpdateCurrentPenModel(Color color, Size size, InkPenType penType, MDToolbarItem penToolbarItem) {
            Logger.Log("[HandwritingTool]", "UpdateCurrentPenModel", color, size, penType, penToolbarItem);

            penToolbarItem.ImageSource = new BitmapImage(new Uri(PenIcons[penType]));
            penToolbarItem.Background = color.ToBrush();
            penToolbarItem.OverrideColor = color.ToBrush();

            if (PenToolbarItems.ContainsValue(penToolbarItem)) {
                var old = PenToolbarItems.Where(kv => kv.Value == penToolbarItem).Select(kv => kv.Key);
                foreach (var oldKey in old.ToArray()) {
                    PenToolbarItems.Remove(oldKey);
                }
            } 
            
            PenToolbarItems.Add(new PenModel(color, size, penType), penToolbarItem);
            try {
                SavePens();
            } catch (Exception ex) {
                Logger.Log("[HandwritingTool]", "UpdateCurrentPenModel - SavePens: Exception", ex.ToString(), logLevel: LogLevel.Warning);
                SentryHelper.CaptureCaughtException(ex);
            }
        }
        

        public static void ShowPenSettingsDropdown(MDToolbarItem penToolbarItemAnchor, Size size, InkPenType penType, Color color) {
            var newSize = size;
            var newPenType = penType;
            var newColor = color;
            
            var sizeSlider = new Slider {
                Maximum = Preferences.MaxPenSize,
                Minimum = Preferences.MinPenSize,
                Value = size.Width,
                Margin = new Thickness(12, 0, 12, 0)
            };
            sizeSlider.ValueChanged += (_, args) => {
                newSize = new Size(args.NewValue, args.NewValue);
                
                ApplyPenModel(new PenModel(newColor, newSize, newPenType));
                UpdateCurrentPenModel(newColor, newSize, newPenType, penToolbarItemAnchor);
            };

            var typePicker = new CustomPenTypePicker(penType) {
                Margin = new Thickness(12, 0, 12, 0)
            };
            typePicker.PenTypeSelected += type => {
                newPenType = type;
                
                ApplyPenModel(new PenModel(newColor, newSize, newPenType));
                UpdateCurrentPenModel(newColor, newSize, newPenType ,penToolbarItemAnchor);
                
                CustomDropdown.CloseDropdown();
            };

            var colorPicker = new CustomColorPicker(color.ToHex(), true) {
                Margin = new Thickness(12, 0, 12, 0)
            };
            colorPicker.ColorSelected += selectedColor => {
                newColor = selectedColor;
                
                ApplyPenModel(new PenModel(newColor, newSize, newPenType));
                UpdateCurrentPenModel(newColor, newSize, newPenType, penToolbarItemAnchor);
                
                CustomDropdown.CloseDropdown();
            };
            CustomDropdown.ShowDropdown(new List<CustomDropdownViewTemplate> {
                new CustomDropdownView(new StackPanel {
                    Padding = new Thickness(0), Margin = new Thickness(0),
                    Children = {
                        sizeSlider,
                        typePicker,
                        new Frame { Height = 4 },
                        colorPicker,
                        new Frame { Height = 4 },
                        new MDButton {
                            ButtonStyle = MDButtonStyle.Secondary,
                            Text = ResourceHelper.GetString("delete"),
                            Margin = new Thickness(12, 0, 12, 0),
                            Command = () => {
                                PenToolbarItems.Remove(PenToolbarItems.ReverseLookup(penToolbarItemAnchor));
                                ((StackPanel) penToolbarItemAnchor.Parent).Children.Remove(penToolbarItemAnchor);
                                SavePens();
                                CustomDropdown.CloseDropdown();
                            }
                        },
                        new Frame { Height = 2 },
                    }
                })
            }, penToolbarItemAnchor, 180);
        }

        public static MDToolbarItem AddPen(PenModel penModel, int index = -1) {
            Logger.Log("[HandwritingTool]", "-> AddPen", penModel, "@", index, logLevel: LogLevel.Debug);
            var penToolbarItem = new MDToolbarItem {
                ImageSource = new BitmapImage(new Uri(PenIcons[penModel.PenType])),
                Background = penModel.Color.ToBrush(),
                OverrideColor = penModel.Color.ToBrush(),
                Deselectable = false,
                Selectable = true,
            };
            penToolbarItem.Pressed += (s, _) => {
                foreach (var (k, v) in PenToolbarItems) {
                    if (v == s) {
                        v.IsSelected = true;
                        ApplyPenModel(k);
                        continue;
                    }
                    v.IsSelected = false;
                }
                
                Logger.Log("Pressed");
            };
            penToolbarItem.PressedAgain += (pressedItem, _) => {
                Logger.Log("PressedAgain");
                foreach (var (model, tbi) in PenToolbarItems) {
                    if (tbi == pressedItem) {
                        penModel = model;
                    }
                }
                ShowPenSettingsDropdown((MDToolbarItem) pressedItem, penModel.Size, penModel.PenType, penModel.Color);
            };
            penToolbarItem.RightPressed += (pressedItem, _) => {
                Logger.Log("RightPressed");
                foreach (var (model, tbi) in PenToolbarItems) {
                    if (tbi == pressedItem) {
                        penModel = model;
                    }
                }
                ShowPenSettingsDropdown((MDToolbarItem) pressedItem, penModel.Size, penModel.PenType, penModel.Color);
            };

            if (PenToolbarItems.ContainsKey(penModel)) {
                PenToolbarItems.Remove(penModel);
            } 
            
            PenToolbarItems.Add(penModel, penToolbarItem);
            
            // insertion logic
            if (App.Page.SecondaryToolbarChildren.Count == 0) {
                App.Page.SecondaryToolbarChildren.Add(penToolbarItem);
            } else if (index == -1) {
                App.Page.SecondaryToolbarChildren.Insert(App.Page.SecondaryToolbarChildren.Count - 1, penToolbarItem);
            } else {
                App.Page.SecondaryToolbarChildren.Insert(index, penToolbarItem);
            }
        
            App.EditorScreen.DoNotRefocus.Add(penToolbarItem);

            return penToolbarItem;
        }
        
        public static void SavePens() {
            PenModels = PenToolbarItems.Keys.Select((itm, idx) => (idx, itm)).ToList();
            
            Logger.Log("[HandwritingComponentToolbarItem]", "SavePens:", PenModels.Count, logLevel: LogLevel.Verbose);
            SavedStatePreferenceHelper.Set("saved_pen_models", JsonConvert.SerializeObject(PenModels));
        }
        
        public void OnDocumentClicked(WTouchEventArgs touchEventArgs, ComponentModel componentModel) { }
    }

    public class CustomPenTypePicker : Grid {
        public const int SwatchSize = CustomColorPicker.SwatchSize;
        public Color HighlightColor = Colors.LightBlue;

        public Action<InkPenType> PenTypeSelected;

        public CustomPenTypePicker(InkPenType selectedPen) {
            ColumnSpacing = RowSpacing = 6;

            RowDefinitions.AddRange(new [] {
                new RowDefinition {Height = new GridLength(SwatchSize)}
            });
            
            ColumnDefinitions.AddRange(new [] {
                new ColumnDefinition {Width = new GridLength(SwatchSize)},
                new ColumnDefinition {Width = new GridLength(SwatchSize)},
                new ColumnDefinition {Width = new GridLength(SwatchSize)},
                new ColumnDefinition {Width = new GridLength(SwatchSize)},
                new ColumnDefinition {Width = new GridLength(SwatchSize)},
            });

            var top = 0;
            var left = 0;
            foreach (InkPenType penType in Enum.GetValues(typeof(InkPenType))) {
                var icon = HandwritingTool.PenIcons[penType];
                var frame = new CustomFrame {
                    Width = SwatchSize,
                    Height = SwatchSize,
                    Background = penType == selectedPen ? HighlightColor.ToBrush() : Background,
                    BorderBrush = Colors.DimGray.ToBrush(),
                    Padding = new Thickness(3),
                    Margin = new Thickness(0),
                    Content = new Image {
                        Source = ImageSourceHelper.FromName(icon),
                    }
                };

                void OnTouch(object o, WTouchEventArgs args)  {
                    if (args.ActionType != WTouchAction.Pressed) return;
                    PenTypeSelected?.Invoke(penType);
                }

                frame.Touch += OnTouch;
                Children.Add(frame, top, left);

                if (left < ColumnDefinitions.Count - 1) {
                    left += 1;
                } else {
                    top += 1;
                    left = 0;
                }
            }
            
            while (Children.Count < 5) {
                var frame = new CustomFrame {
                    Width = Height = SwatchSize,
                    BorderBrush = Background,
                    Background = Background,
                };
                Children.Add(frame, top, left);
                
                if (left < ColumnDefinitions.Count - 1) {
                    left += 1;
                } else {
                    top += 1;
                    left = 0;
                }
            }

            Height = RowDefinitions.Count * (SwatchSize + RowSpacing) - RowSpacing;
        }
    }
}