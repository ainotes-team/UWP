using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AINotes.Helpers;
using Helpers.Extensions;
using MaterialComponents;

namespace AINotes.Controls.Sidebar.Content {
    public class SidebarThesaurus : Frame, ISidebarView {
        public IEnumerable<MDToolbarItem> ExtraButtons { get; } = new MDToolbarItem[] { };

        private MDEntry _searchBar;
        private ScrollViewer _resultScroll;
        private StackPanel _resultPanel;

        private readonly Dictionary<string, ListView> _resultListViews = new Dictionary<string, ListView>();
        private readonly Dictionary<string, ObservableCollection<string>> _resultSources = new Dictionary<string, ObservableCollection<string>>();

        private bool _isFirstOverride = true;

        private readonly SemaphoreSlim _textChangedSemaphore = new SemaphoreSlim(1, 1);

        protected override Size ArrangeOverride(Size finalSize) {
            if (!_isFirstOverride) return base.ArrangeOverride(finalSize);
            
            _searchBar = new MDEntry {
                Placeholder = "Suchbegriff",
                TextWrapping = TextWrapping.NoWrap,
            };

            _resultPanel = new StackPanel();
            _resultScroll = new ScrollViewer {
                Content = _resultPanel,
            };

            Content = new Grid {
                ColumnDefinitions = {
                    new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)}
                },
                RowDefinitions = {
                    new RowDefinition {Height = new GridLength(48, GridUnitType.Pixel)},
                    new RowDefinition {Height = new GridLength(1, GridUnitType.Star)}
                },
                Children = {
                    {_searchBar, 0, 0},
                    {_resultScroll, 1, 0},
                }
            };

            _searchBar.TextChanged += OnSearchBarTextChanged;

            _isFirstOverride = false;
            return base.ArrangeOverride(finalSize);
        }

        private async void OnSearchBarTextChanged(object sender, TextChangedEventArgs args) {
            try {
                await _textChangedSemaphore.WaitAsync();
                if (_searchBar.Text.Length == 0) {
                    _resultPanel?.Children.Clear();
                    _resultListViews.Clear();
                    _resultSources.Clear();
                    return;
                }

                var synonyms = await ThesaurusHelper.FindSynonyms(_searchBar.Text);
                foreach (var synonym in synonyms) {
                    if (_resultSources.ContainsKey(synonym.Word)) {
                        if (_resultSources[synonym.Word].Contains(synonym.Synonym.FirstCharToUpper())) continue;
                        _resultSources[synonym.Word].Add(synonym.Synonym.FirstCharToUpper());
                    } else {
                        _resultSources.Add(synonym.Word, new ObservableCollection<string>());
                        var lv = new ListView {ItemsSource = _resultSources[synonym.Word], Header = synonym.Word.FirstCharToUpper(),};

                        _resultPanel?.Children.Add(lv);
                        _resultListViews.Add(synonym.Word, lv);

                        _resultSources[synonym.Word].Add(synonym.Synonym.FirstCharToUpper());
                    }
                }

                foreach (var (sKey, _) in _resultSources.ToList().Where(sKey => synonyms.All(itm => itm.Word != sKey.Key))) {
                    _resultPanel?.Children.Remove(_resultListViews[sKey]);
                    _resultSources.Remove(sKey);
                    _resultListViews.Remove(sKey);
                }
            } finally {
                _textChangedSemaphore.Release();
            }
        }
    }
}