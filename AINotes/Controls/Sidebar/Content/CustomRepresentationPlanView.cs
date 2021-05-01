using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using AINotes.Helpers.Sidebar.RepresentationPlan;
using AINotes.Helpers.Sidebar.RepresentationPlan.Models;
using Helpers.Extensions;
using MaterialComponents;
using System;

namespace AINotes.Controls.Sidebar.Content {
    public class CustomRepresentationPlanView : Frame, ISidebarView {
        private static readonly MDToolbarItem ReloadItem = new MDToolbarItem {
            ImageSource = new BitmapImage(new Uri(Icon.Reset)),
        };

        public IEnumerable<MDToolbarItem> ExtraButtons { get; } = new List<MDToolbarItem> {
            ReloadItem,
        };

        private ScrollViewer _resultScroll;

        private readonly Dictionary<string, ListView> _resultListViews = new Dictionary<string, ListView>();
        private readonly Dictionary<string, ObservableCollection<RepresentationItemModel>> _resultSources = new Dictionary<string, ObservableCollection<RepresentationItemModel>>();

        private async void UpdateRepresentations() {
            var dayRepresentationsDict = await RepresentationPlanManager.GetRepresentations();
            foreach (var (key, values) in dayRepresentationsDict) {
                if (_resultSources.ContainsKey(key)) {
                    // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                    foreach (var representation in values) {
                        if (_resultSources[key].Contains(representation)) continue;
                        _resultSources[key].Add(representation);
                    }

                    // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                    foreach (var oldRepresentation in _resultSources[key].ToList()) {
                        if (!values.Contains(oldRepresentation)) {
                            _resultSources[key].Remove(oldRepresentation);
                        }
                    }
                } else {
                    _resultSources.Add(key, new ObservableCollection<RepresentationItemModel>());
                    var lv = new ListView {
                        ItemsSource = _resultSources[key],
                        Header = key,
                        ItemTemplate = SidebarContentResources.RepresentationItemModelTemplate,
                        SelectionMode = ListViewSelectionMode.None,
                    };

                    ((StackPanel) _resultScroll.Content)?.Children.Add(lv);
                    _resultListViews.Add(key, lv);

                    foreach (var representation in values) {
                        _resultSources[key].Add(representation);
                    }
                }
            }

            foreach (var (sKey, _) in _resultSources.ToList().Where(sKey => dayRepresentationsDict.All(itm => itm.Key != sKey.Key))) {
                ((StackPanel) _resultScroll.Content)?.Children.Remove(_resultListViews[sKey]);
                _resultSources.Remove(sKey);
                _resultListViews.Remove(sKey);
            }
        }

        private bool _isFirstOverride = true;

        protected override Size ArrangeOverride(Size finalSize) {
            if (!_isFirstOverride) return base.ArrangeOverride(finalSize);

            Content = new Grid {
                ColumnDefinitions = {
                    new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)}
                },
                RowDefinitions = {
                    new RowDefinition {Height = new GridLength(1, GridUnitType.Star)}
                }
            };

            ((Grid) Content).Children.Add(_resultScroll = new ScrollViewer {
                Content = new StackPanel(),
                // MaxWidth = CustomContentPage.SidebarWidth - 130
            }, 0, 0);

            ReloadItem.Released += (_, _) => UpdateRepresentations();
            UpdateRepresentations();

            _isFirstOverride = false;
            return base.ArrangeOverride(finalSize);
        }
    }
}