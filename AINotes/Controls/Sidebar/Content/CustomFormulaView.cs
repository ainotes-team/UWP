using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using AINotes.Controls.Pages;
using AINotes.Helpers.Sidebar;
using Helpers.Extensions;
using MaterialComponents;
using Newtonsoft.Json;

namespace AINotes.Controls.Sidebar.Content {
    public class FormulaItem {
        [JsonProperty]
        public string Title { get; set; }

        [JsonProperty]
        public List<string> Formulas { get; set; }

        public event Action Changed;

        private UIElement _currentView;

        private CustomFormulaView _formulaView;
        private MDEntry _titleEntry;
        private MDToolbarItem _menuTBI;
        private StackPanel _stack;
        private StackPanel _formulaStack;

        public UIElement GetView(CustomFormulaView formulaView) {
            if (_currentView != null) return _currentView;
            _formulaView = formulaView;
            _stack = new StackPanel();
            
            _titleEntry = new MDEntry {
                Margin = new Thickness(0, 0, 0, 12),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 270,
                Placeholder = "Collection Title",
                Text = Title ?? ""
            };

            _menuTBI = new MDToolbarItem {
                ImageSource = new BitmapImage(new Uri(Icon.MenuVertical)),
                VerticalAlignment = VerticalAlignment.Center,
            };
            
            _formulaStack = new StackPanel();

            _titleEntry.TextChanged += OnTitleEntryTextChanged;

            _menuTBI.Released += OnMenuTBIReleased;
            
            _stack.Children.Add(new StackPanel {
                Orientation = Orientation.Horizontal,
                Children = {
                    _titleEntry,
                    _menuTBI
                }
            });
            
            _stack.Children.Add(_formulaStack);
            
            Formulas ??= new List<string>();

            foreach (var f in Formulas.Select(formula => new MDEntry(false) {
                Text = formula
            })) {
                f.TextChanged += UpdateString;
                
                _formulaStack.Children.Add(f);
            }
            
            var d = new Frame {
                Content = _stack,
                Margin = new Thickness(12, 12, 12, 30),
                Padding = new Thickness(6),
                CornerRadius = new CornerRadius(4),
                BorderBrush = new SolidColorBrush(Colors.LightGray),
                BorderThickness = new Thickness(1.5)
            };

            _currentView = d;
            return d;
        }

        private void OnTitleEntryTextChanged(object sender, TextChangedEventArgs args) {
            Title = _titleEntry.Text;
            Changed?.Invoke();
        }
        
        private void UpdateString(object _, object __) {
            Formulas.Clear();
            foreach (var formula in _formulaStack.Children) {
                Formulas.Add(((MDEntry) formula).Text);
            }
                
            Changed?.Invoke();
        }

        private void OnMenuTBIReleased(object s, EventArgs e) {
            CustomDropdown.ShowDropdown(new[] {
                new CustomDropdownItem("Add", () => {
                    var f = new MDEntry(false);

                    f.TextChanged += UpdateString;

                    _formulaStack.Children.Add(f);
                }),
                new CustomDropdownItem("Remove", () => {
                    _formulaView.FormulaItems.Remove(this);
                }),
            }, s as MDToolbarItem);
        }
    }
    
    public class CustomFormulaView : Frame, ISidebarView {
        private IEnumerable<MDToolbarItem> _extraButtons;
        public IEnumerable<MDToolbarItem> ExtraButtons {
            get {
                if (_extraButtons != null) return _extraButtons;
                
                var addTBI = new MDToolbarItem {
                    ImageSource = new BitmapImage(new Uri(Icon.Add)),
                };
                addTBI.Released += (_, _) => {
                    var newItem = new FormulaItem();
                    newItem.Changed += Save;
                    FormulaItems.Add(newItem);
                };
                    
                _extraButtons = new List<MDToolbarItem> {
                    addTBI
                };

                return _extraButtons;
            }
        }

        public const string DefaultValue = "[]";

        public readonly ObservableCollection<FormulaItem> FormulaItems = new ObservableCollection<FormulaItem>();
        private bool _isFirstOverride = true;

        private readonly MDEntry _searchEntry;
        private readonly StackPanel _stack;
        public CustomFormulaView() {
            _searchEntry = new MDEntry {
                Placeholder = "Search",
                Margin = new Thickness(8)
            };
            
            _searchEntry.TextChanged += OnSearchEntryTextChanged;

            _stack = new StackPanel {
                Transitions = new TransitionCollection {new EntranceThemeTransition()}
            };

            Content = new ScrollViewer {
                Content = new StackPanel {
                    Orientation = Orientation.Vertical,
                    Children = {
                        _searchEntry,
                        _stack
                    }
                }
            };

            FormulaItems.CollectionChanged += OnFormulaItemsCollectionChanged;
        }

        private void OnFormulaItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args) {
            if (args.NewItems != null) {
                foreach (var view in args.NewItems) {
                    _stack.Children.Add(((FormulaItem) view).GetView(this));
                    ((FormulaItem) view).Changed += Save;
                }
            }

            if (args.OldItems != null) {
                foreach (var view in args.OldItems) {
                    _stack.Children.Remove(((FormulaItem) view).GetView(this));
                }
            }

            Save();
        }

        private void OnSearchEntryTextChanged(object sender, TextChangedEventArgs args) {
            Search(_searchEntry.Text.ToLower());
        }

        private void Search(string searchString) {
            if (searchString == "") {
                foreach (var item in FormulaItems) {
                    item.GetView(this).Visibility = Visibility.Collapsed;
                }
            }

            foreach (var item in FormulaItems) {
                item.GetView(this).Visibility = item.Title.ToLower().Contains(searchString) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        
        protected override Size ArrangeOverride(Size finalSize) {
            if (!_isFirstOverride) return base.ArrangeOverride(finalSize);
            _isFirstOverride = false;
            
            foreach (var item in Load()) {
                FormulaItems.Add(item);
            }
            
            return base.ArrangeOverride(finalSize);
        }

        private IEnumerable<FormulaItem> Load() {
            var loaded = SidebarHelper.GetFormulasJson();
            if (loaded != "" && loaded != DefaultValue) return loaded.Deserialize<List<FormulaItem>>();

            return new List<FormulaItem>();
        }

        public void Save() {
            SidebarHelper.SetFormulasJson(FormulaItems.ToList().Serialize());
        }
    }
}