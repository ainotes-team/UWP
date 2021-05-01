using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Helpers.Lists {
    public sealed class ObservableList<T> : ObservableCollection<T> {
        public bool PreventDuplicates { get; set; }

        private Func<T, object> _secondarySortingSelector;
        public Func<T, object> SecondarySortingSelector {
            get => _secondarySortingSelector;
            set {
                _secondarySortingSelector = value;
                Sort();
            }
        }

        private Func<T, object> _sortingSelector;
        public Func<T, object> SortingSelector {
            get => _sortingSelector;
            set {
                _sortingSelector = value;
                Sort();
            }
        }

        private bool _descending;
        public bool Descending {
            get => _descending;
            set {
                _descending = value;
                Sort();
            }
        }
        
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
            base.OnCollectionChanged(e);
            if (SecondarySortingSelector == null || e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset) return;
            Sort();
        }

        public void Sort() {
            if (SortingSelector == null || SecondarySortingSelector == null) return;
            var query = this.Select((item, index) => (Item: item, Index: index));
            query = Descending ? query.OrderBy(tuple => SortingSelector(tuple.Item)).ThenBy(tuple => SecondarySortingSelector(tuple.Item)) : query.OrderBy(tuple => SortingSelector(tuple.Item)).ThenByDescending(tuple => SecondarySortingSelector(tuple.Item));

            var map = query.Select((tuple, index) => (OldIndex:tuple.Index, NewIndex:index)).Where(o => o.OldIndex != o.NewIndex);

            using (var enumerator = map.GetEnumerator()) {
                if (enumerator.MoveNext()) {
                    Move(enumerator.Current.OldIndex, enumerator.Current.NewIndex);
                }
            }
        }
        
        protected override void InsertItem(int index, T item) {
            if (item == null || Contains(item) && PreventDuplicates) return;
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, T item) {
            if (item == null || Contains(item) && PreventDuplicates) return;
            base.SetItem(index, item);
        }
    }
}