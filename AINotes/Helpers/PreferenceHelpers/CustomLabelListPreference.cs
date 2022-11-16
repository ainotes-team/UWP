using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using AINotes.Controls.Input;
using AINotes.Controls.Popups;
using AINotes.Models;
using Helpers;
using Helpers.Controls;
using Helpers.Essentials;
using Helpers.Extensions;
using MaterialComponents;

namespace AINotes.Helpers.PreferenceHelpers {
    public class CustomLabelListPreference : Preference {
        private StackPanel _view;

        public CustomLabelListPreference(string displayName, [CallerMemberName] string propertyName = "") : base(propertyName, displayName, null) { }

        public event Action LabelListChanged;
        
        private void AddLabel() {
            var nameEntry = new MDEntry {
                Placeholder = "Name",
            };

            var colorPicker = new CustomColorPicker {
                DefaultColors = new ArrayList(new Dictionary<string, Color> {
                    {"none", ColorCreator.FromHex("#FFFFFF")},
                    {"maths", ColorCreator.FromHex("#FCB450")},
                    {"german", ColorCreator.FromHex("#5851DB")},
                    {"english", ColorCreator.FromHex("#5CBC63")},
                    {"latin", ColorCreator.FromHex("#FC5650")},
                    {"french", ColorCreator.FromHex("#E56588")},
                    {"physics", ColorCreator.FromHex("#5D9EBE")},
                    {"biology", ColorCreator.FromHex("#96C03A")},
                    {"chemistry", ColorCreator.FromHex("#963AC0")},
                    {"informatics", ColorCreator.FromHex("#898F8F")},
                    {"history", ColorCreator.FromHex("#AE8B65")},
                    {"socialstudies", ColorCreator.FromHex("#B0DDF0")},
                    {"geography", ColorCreator.FromHex("#FAC9A1")},
                    {"religion", ColorCreator.FromHex("#4E5357")},
                    {"philosophy", ColorCreator.FromHex("#7D82BA")},
                    {"music", ColorCreator.FromHex("#FEF2B6")},
                    {"art", ColorCreator.FromHex("#FAE355")},
                }.Values)
            };

            var statusLabel = new MDLabel {
                Height = 0,
                Foreground = Colors.Red.ToBrush(),
            };

            var selectedColor = Colors.Transparent;
            colorPicker.ColorSelected += color => {
                selectedColor = color;
                foreach (var v in colorPicker.Children) {
                    if (v is not CustomFrame f) continue;
                    if (f.Background != selectedColor.ToBrush()) {
                        f.BorderBrush = Configuration.Theme.CardBorder;
                        f.BorderThickness = new Thickness(1);
                    } else {
                        f.BorderBrush = Colors.Black.ToBrush();
                        f.BorderThickness = new Thickness(3);
                    }
                }
            };

            new MDContentPopup("Change Subject", new StackPanel {
                Children = {
                    nameEntry,
                    colorPicker,
                    statusLabel
                }
            }, async () => {
                if (selectedColor == Colors.Transparent || string.IsNullOrWhiteSpace(nameEntry.Text)) {
                    statusLabel.Text = "Please choose a subject and color.";
                    statusLabel.Height = double.NaN;
                    return;
                }

                var newItem = new LabelModel {
                    Name = nameEntry.Text,
                    Color = selectedColor,
                };

                newItem.LabelId = await FileHelper.CreateLabelAsync(newItem);
                AddLabelFrame(newItem, 1);
                MDPopup.CloseCurrentPopup();
                LabelListChanged?.Invoke();
            }, true, true, null, false, "Create", "Cancel").Show();
        }

        private void EditLabel(LabelModel model) {
            var nameEntry = new MDEntry {
                Text = model.Name,
            };

            var colorPicker = new CustomColorPicker {
                DefaultColors = new ArrayList(new Dictionary<string, Color> {
                    {"none", ColorCreator.FromHex("#FFFFFF")},
                    {"maths", ColorCreator.FromHex("#FCB450")},
                    {"german", ColorCreator.FromHex("#5851DB")},
                    {"english", ColorCreator.FromHex("#5CBC63")},
                    {"latin", ColorCreator.FromHex("#FC5650")},
                    {"french", ColorCreator.FromHex("#E56588")},
                    {"physics", ColorCreator.FromHex("#5D9EBE")},
                    {"biology", ColorCreator.FromHex("#96C03A")},
                    {"chemistry", ColorCreator.FromHex("#963AC0")},
                    {"informatics", ColorCreator.FromHex("#898F8F")},
                    {"history", ColorCreator.FromHex("#AE8B65")},
                    {"socialstudies", ColorCreator.FromHex("#B0DDF0")},
                    {"geography", ColorCreator.FromHex("#FAC9A1")},
                    {"religion", ColorCreator.FromHex("#4E5357")},
                    {"philosophy", ColorCreator.FromHex("#7D82BA")},
                    {"music", ColorCreator.FromHex("#FEF2B6")},
                    {"art", ColorCreator.FromHex("#FAE355")},
                }.Values)
            };

            var statusLabel = new MDLabel {
                Height = 0,
                Foreground = Colors.Red.ToBrush(),
            };

            var selectedColor = model.Color;
            UpdateSelectedColor(selectedColor);

            void UpdateSelectedColor(Color color) {
                selectedColor = color;
                foreach (var v in colorPicker.Children) {
                    if (v is not CustomFrame f) continue;
                    if (f.Background != selectedColor.ToBrush()) {
                        f.BorderBrush = Configuration.Theme.CardBorder;
                        f.BorderThickness = new Thickness(1);
                    } else {
                        f.BorderBrush = Colors.Black.ToBrush();
                        f.BorderThickness = new Thickness(3);
                    }
                }
            }

            colorPicker.ColorSelected += UpdateSelectedColor;

            new MDContentPopup("Change subject", new StackPanel {
                Children = {
                    nameEntry,
                    colorPicker
                }
            }, async () => {
                if (selectedColor == Colors.Transparent || string.IsNullOrWhiteSpace(nameEntry.Text)) {
                    statusLabel.Text = "Please choose a subject and color.";
                    statusLabel.Height = 0;
                    return;
                }

                var newItem = new LabelModel {
                    Name = nameEntry.Text,
                    Color = selectedColor,
                    LabelId = model.LabelId
                };

                await FileHelper.UpdateLabelAsync(newItem);

                try {
                    var collectionA = ((StackPanel) ((StackPanel) _subjectFrames[model].Content)?.Children[0])?.Children;
                    if (collectionA != null) {
                        ((CustomFrame) collectionA[0]).Background = selectedColor.ToBrush();
                        ((MDLabel) ((StackPanel) ((StackPanel) _subjectFrames[model].Content).Children[0]).Children[1]).Text = nameEntry.Text;
                    }

                    var collectionB = ((StackPanel) ((StackPanel) _subjectFrames[model].Content)?.Children[1])?.Children;
                    if (collectionB != null) {
                        ((MDToolbarItem) collectionB[0]).Pressed += (_, _) => EditLabel(newItem);
                        ((MDToolbarItem) ((StackPanel) ((StackPanel) _subjectFrames[model].Content).Children[1]).Children[1]).Released += (_, _) => DeleteLabel(newItem);
                    }
                } catch (InvalidCastException ex) {
                    Logger.Log("[CustomLabelListPreference]", "EditLabels: UI Updated failed (InvalidCastException):", ex, logLevel: LogLevel.Error);
                }

                var frame = _subjectFrames[model];

                _subjectFrames.Remove(model);
                _subjectFrames.Add(newItem, frame);

                MDPopup.CloseCurrentPopup();
                LabelListChanged?.Invoke();
            }, cancelable: true, closeOnOk: false).Show();
        }

        private async void DeleteLabel(LabelModel model) {
            _view.Children.Remove(_subjectFrames[model]);
            _subjectFrames[model] = null;
            _subjectFrames.Remove(model);
            await FileHelper.DeleteLabelAsync(model);
            LabelListChanged?.Invoke();
        }

        private async void ToggleArchiveLabel(LabelModel model) {
            _view.Children.Remove(_subjectFrames[model]);
            _subjectFrames[model] = null;
            _subjectFrames.Remove(model);
            model.Archived = !model.Archived;
            await FileHelper.UpdateLabelAsync(model);
            LabelListChanged?.Invoke();
            AddLabelFrame(model, _subjectFrames.Count+1);
        }

        private readonly Dictionary<LabelModel, CustomFrame> _subjectFrames = new Dictionary<LabelModel, CustomFrame>();

        public override UIElement GetView() {
            if (_view != null) return _view;

            _view = new StackPanel {
                Background = Configuration.Theme.Background,
            };

            // add button
            var addSubjectFrame = new CustomFrame {
                Height = 48, 
                CornerRadius = new CornerRadius(8),
                Content = new StackPanel {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Children = {
                        new MDToolbarItem {
                            ImageSource = new BitmapImage(new Uri(Icon.Add)),
                            Height = 32,
                            Width = 32,
                            IsEnabled = false
                        },
                        new MDLabel {
                            TextAlignment = TextAlignment.Center,
                            HorizontalTextAlignment = TextAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Text = "Add subject"
                        }
                    }
                }
            };

            addSubjectFrame.Touch += (_, args) => {
                switch (args.ActionType) {
                    case WTouchAction.Entered:
                        addSubjectFrame.Background = Configuration.Theme.ToolbarItemHover;
                        break;
                    case WTouchAction.Released:
                        MainThread.BeginInvokeOnMainThread(() => addSubjectFrame.Background = Configuration.Theme.ToolbarItemTap);
                        AddLabel();
                        break;
                    case WTouchAction.Pressed:
                        addSubjectFrame.Background = Configuration.Theme.ToolbarItemTap;
                        break;
                    case WTouchAction.Cancelled:
                    case WTouchAction.Exited:
                        addSubjectFrame.Background = Configuration.Theme.Background;
                        break;
                }
            };

            _view.Children.Add(addSubjectFrame);

            // subjects
            Task.Run(async () => {
                var items = await FileHelper.ListLabelsAsync();
                foreach (var itm in items.Where(itm => !itm.Archived)) {
                    MainThread.BeginInvokeOnMainThread(() => AddLabelFrame(itm));
                }
                foreach (var itm in items.Where(itm => itm.Archived)) {
                    MainThread.BeginInvokeOnMainThread(() => AddLabelFrame(itm));
                }
            });

            return _view;
        }

        private void AddLabelFrame(LabelModel itm, int atIndex = -1) {
            var deleteTBI = new MDToolbarItem {
                ImageSource = new BitmapImage(new Uri(Icon.TrashCan)),
                Width = 32,
                Height = 32,
                ToolTip = "Delete"
            };
            
            var archiveTBI = new MDToolbarItem {
                ImageSource = new BitmapImage(new Uri(Icon.Expired)),
                Width = 32,
                Height = 32,
                ToolTip = "Archive"
            };

            deleteTBI.Released += (_, _) => DeleteLabel(itm);
            archiveTBI.Released += (_, _) => ToggleArchiveLabel(itm);
            
            var contentStack = new StackPanel {
                Orientation = Orientation.Horizontal,
                Height = 48,
                Children = {
                    new StackPanel {
                        Orientation = Orientation.Horizontal,
                        VerticalAlignment = VerticalAlignment.Center,
                        Children = {
                            new CustomFrame {
                                Height = 24,
                                Width = 24,

                                CornerRadius = new CornerRadius(48),

                                Background = itm.Color.ToBrush(),

                                Padding = new Thickness(6, 6, 6, 6),
                                Margin = new Thickness(6, 6, 6, 6),
                            },
                            new MDLabel {
                                VerticalAlignment = VerticalAlignment.Center,
                                Text = itm.Name,
                                Foreground = itm.Archived ? Theming.CurrentTheme.Text.AddLuminosity(0.5) : Theming.CurrentTheme.Text
                            }
                        }
                    },
                    deleteTBI,
                    archiveTBI
                }
            };

            var contentFrame = new CustomFrame {
                Margin = new Thickness(4),
                CornerRadius = new CornerRadius(8),
                // InputTransparent = false,
                Content = contentStack,
            };

            contentFrame.Touch += (_, args) => {
                switch (args.ActionType) {
                    case WTouchAction.Entered:
                        contentFrame.Background = Configuration.Theme.ToolbarItemHover;
                        break;
                    case WTouchAction.Released:
                        EditLabel(itm);
                        break;
                    case WTouchAction.Pressed:
                        contentFrame.Background = Configuration.Theme.ToolbarItemTap;
                        break;
                    case WTouchAction.Cancelled:
                    case WTouchAction.Exited:
                        contentFrame.Background = Configuration.Theme.Background;
                        break;
                }
            };

            if (atIndex == -1) {
                _view.Children.Add(contentFrame);
            } else {
                _view.Children.Insert(atIndex, contentFrame);
            }

            _subjectFrames[itm] = contentFrame;
        }
    }
}