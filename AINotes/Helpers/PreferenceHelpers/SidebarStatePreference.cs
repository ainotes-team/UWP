using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Helpers.Essentials;
using Helpers.Extensions;
using MaterialComponents;

namespace AINotes.Helpers.PreferenceHelpers {
    public class SidebarStatePreference : Preference {
        private readonly Dictionary<Type, (bool, bool)> _defaultValue;
        private Grid _view;
        
        public SidebarStatePreference(string displayName, Dictionary<Type, (bool, bool)> defaultValue=null, Action onChanged=null, [CallerMemberName] string propertyName = "") : base(propertyName, displayName, onChanged) {
            _defaultValue = defaultValue;
        }
        
        private Dictionary<Type, (bool, bool)> GetValue() => UserPreferenceHelper.Get(PropertyName, _defaultValue.Serialize()).Deserialize<Dictionary<Type, (bool, bool)>>();
        
        public override UIElement GetView() {
            if (_view != null) return _view;
            
            _view = new Grid {
                ColumnDefinitions = {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                },
                RowDefinitions = {
                    new RowDefinition { Height = GridLength.Auto },
                },
            };
            
            _view.Children.Add(new Border {
                Child = new MDLabel("Screen"),
                BorderThickness = new Thickness(0, 0, 0, 0),
                BorderBrush = Colors.Black.ToBrush()
            }, 0, 0);
            _view.Children.Add(new Border {
                Child = new MDLabel("Linke Sidebar"),
                BorderThickness = new Thickness(1, 0, 0, 0),
                BorderBrush = Colors.Black.ToBrush()
            }, 0, 1);
            _view.Children.Add(new Border {
                Child = new MDLabel("Rechte Sidebar"),
                BorderThickness = new Thickness(1, 0, 0, 0),
                BorderBrush = Colors.Black.ToBrush()
            }, 0, 2);

            var currentValue = GetValue();
            
            var screenBoxDict = new Dictionary<Type, (CheckBox, CheckBox)>();

            void Save() {
                var result = new Dictionary<Type, (bool, bool)>();
                foreach (var (type, (lBox, rBox)) in screenBoxDict) {
                    result.Add(type, (lBox.IsChecked ?? false, rBox.IsChecked ?? false));
                }

                UserPreferenceHelper.Set(PropertyName, result.Serialize());
            }

            foreach (var screenType in currentValue.Keys) {
                var (left, right) = currentValue[screenType];
                _view.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var lBox = new CheckBox {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    IsChecked = left,
                };

                var rBox = new CheckBox {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    IsChecked = right,
                };
                
                screenBoxDict.Add(screenType, (lBox, rBox));
                
                _view.Children.Add(new Border {
                    Child = new MDLabel {
                        Text = screenType.Name, 
                        TextAlignment = TextAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    BorderThickness = new Thickness(0, 1, 0, 0),
                    BorderBrush = Colors.Black.ToBrush()
                }, _view.RowDefinitions.Count - 1, 0);
                _view.Children.Add(new Border {
                    Child = lBox, 
                    BorderThickness = new Thickness(1, 1, 0, 0),
                    BorderBrush = Colors.Black.ToBrush()
                }, _view.RowDefinitions.Count - 1, 1);
                _view.Children.Add(new Border {
                    Child = rBox,
                    BorderThickness = new Thickness(1, 1, 0, 0),
                    BorderBrush = Colors.Black.ToBrush()
                }, _view.RowDefinitions.Count - 1, 2);

                void OnSaveProvoked(object _, object __) {
                    Save();
                }
                
                lBox.Checked += OnSaveProvoked;
                lBox.Unchecked += OnSaveProvoked;
                
                rBox.Checked += OnSaveProvoked;
                rBox.Unchecked += OnSaveProvoked;
            }
            
            return _view;
        }
        
        public static implicit operator Dictionary<Type, (bool, bool)>(SidebarStatePreference x) => x.GetValue();
    }
}