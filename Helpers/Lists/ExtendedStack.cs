using System.Collections.Generic;

namespace Helpers.Lists {
    public class ExtendedStack<T> : List<T> {
        public void Push(T item) {
            Add(item);
        }
        
        public T Pop() {
            if (Count <= 0) return default;
            var temp = this[Count - 1];
            RemoveAt(Count - 1);
            return temp;
        }
    }
}