using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using AINotes.Helpers.Sidebar;
using Helpers;
using Helpers.Extensions;
using MaterialComponents;
using Newtonsoft.Json;

namespace AINotes.Controls.Sidebar.Content {
    public enum TimeTableMode {
        Grid,
        List
    }
    
    public class TimeTableItem {
        [JsonProperty]
        public string Text { get; set; }

        [JsonProperty]
        public int Left { get; set; }

        [JsonProperty]
        public int Top { get; set; }

        public TimeTableItem(string text, int left, int top) {
            Text = text;
            Left = left;
            Top = top;
        }

        public event Action TextChanged;

        private MDEntry _entry;
        public MDEntry GetView() {
            _entry = new MDEntry {
                Text = Text,
                Width = 300,
                Margin = new Thickness(2)
            };

            _entry.TextChanged += OnTextChanged;

            return _entry;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs args) {
            Text = _entry.Text;
            TextChanged?.Invoke();
        }
    }
    
    
    public class CustomTimeTableView : Frame, ISidebarView {
        private readonly ObservableCollection<TimeTableItem> _timeTableSource = new ObservableCollection<TimeTableItem>();

        private TimeTableMode _mode;
        
        private readonly Dictionary<int, string> _daysOfWeek = new Dictionary<int, string> {
            {0, ResourceHelper.GetString("day_monday")},
            {1, ResourceHelper.GetString("day_tuesday")},
            {2, ResourceHelper.GetString("day_wednesday")},
            {3, ResourceHelper.GetString("day_thursday")},
            {4, ResourceHelper.GetString("day_friday")},
        };
        
        public TimeTableMode Mode {
            get => _mode;
            set {
                _mode = value;
                
                _grid.Children.Clear();
                _grid.RowDefinitions.Clear();
                _grid.ColumnDefinitions.Clear();
                
                switch (value) {
                    case TimeTableMode.Grid:
                        for (var hour = 0; hour < 13; hour++) {
                            _grid.RowDefinitions.Add(new RowDefinition {
                                Height = new GridLength(50)
                            });
                        }

                        for (var day = 0; day < 5; day++) {
                            _grid.ColumnDefinitions.Add(new ColumnDefinition {
                                Width = new GridLength(1, GridUnitType.Star)
                            });
                        }

                        foreach (var item in _timeTableSource) {
                            _grid.Children.Add(item.GetView(), item.Top, item.Left);
                        }
                        break;
                    case TimeTableMode.List:
                        for (var day = 0; day < 5; day++) {
                            _grid.RowDefinitions.Add(new RowDefinition {
                                Height = new GridLength(1, GridUnitType.Auto)
                            });
                            
                            _grid.RowDefinitions.Add(new RowDefinition {
                                Height = new GridLength(1, GridUnitType.Auto)
                            });
                        }
                        
                        _grid.ColumnDefinitions.Add(new ColumnDefinition {
                            Width = new GridLength(1, GridUnitType.Star)
                        });

                        foreach (var (dayCount, dayName) in _daysOfWeek) {
                            var stack = new StackPanel {
                                Transitions = new TransitionCollection {
                                    new EntranceThemeTransition()
                                }
                            };

                            foreach (var item in _timeTableSource.Where(item => item.Left == dayCount)) {
                                stack.Children.Add(new StackPanel {
                                    Orientation = Orientation.Horizontal,
                                    Children = {
                                        new MDLabel(item.Top < 9 ? "0" + (item.Top + 1) : (item.Top + 1).ToString()) {
                                            Margin = new Thickness(10),
                                            VerticalAlignment = VerticalAlignment.Center,
                                        },
                                        item.GetView()
                                    }
                                });
                            }

                            var title = new MDLabel(dayName) {
                                Margin = new Thickness(10)
                            };
                            Grid.SetColumnSpan(title, 2);
                            
                            _grid.Children.Add(title, dayCount * 2, 0);
                            _grid.Children.Add(stack, dayCount * 2 + 1, 0);
                        }
                        
                        break;
                }
            }
        }

        private readonly Grid _grid = new Grid();
        
        private IEnumerable<MDToolbarItem> _extraButtons;

        public IEnumerable<MDToolbarItem> ExtraButtons {
            get {
                if (_extraButtons != null) return _extraButtons;

                var modeItem = new MDToolbarItem {
                    ImageSource = new BitmapImage(new Uri(Icon.List)),
                };
                modeItem.Released += OnModeItemReleased;

                _extraButtons = new List<MDToolbarItem> {
                    modeItem
                };

                return _extraButtons;
            }
        }

        private void OnModeItemReleased(object s, EventArgs e) {
            Mode = Mode == TimeTableMode.Grid ? TimeTableMode.List : TimeTableMode.Grid;
        }

        public const string DefaultValue = "[]";

        public CustomTimeTableView() {
            Content = new ScrollViewer {
                Content = _grid,
            };
            
            Transitions = new TransitionCollection {
                new EntranceThemeTransition()
            };
            
            _timeTableSource.AddRange(Load());

            foreach (var item in _timeTableSource.ToArray()) {
                item.TextChanged += Save;
            }

            Mode = TimeTableMode.List;
        }
        
        private IEnumerable<TimeTableItem> Load() {
            var loaded = SidebarHelper.GetTimetableJson();
            try {
                if (loaded != "" && loaded != DefaultValue) {
                    var des = loaded.Deserialize<List<TimeTableItem>>();
                    return des;
                }
            
                var initItems = new List<TimeTableItem>();
                for (var day = 0; day < 5; day++) {
                    for (var hour = 0; hour < 13; hour++) {
                        initItems.Add(new TimeTableItem("", day, hour));
                    }
                }

                return initItems;
            } catch (Exception ex) {
                Logger.Log("[CustomTimeTableView]", "Exception in Load", ex.ToString(), logLevel: LogLevel.Error);
                return new List<TimeTableItem>();
            }
        }

        public void Save() {
            SidebarHelper.SetTimetableJson(_timeTableSource.ToList().Serialize());
        }
    }
}