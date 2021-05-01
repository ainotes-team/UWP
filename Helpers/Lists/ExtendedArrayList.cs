using System.Collections;
using Helpers.Extensions;

namespace Helpers.Lists {
    public class ExtendedArrayList : ArrayList {
        public int Limit { get; set; } = 0;
        public bool PreventDuplicates { get; set; } = false;

        public override int Add(object obj) {
            if (PreventDuplicates && Contains(obj)) return 0;
            if (Limit > 0 && Count >= Limit) RemoveAt(0);
            return base.Add(obj);
        }

        public override string ToString() => $"CustomArrayList: [{", ".Join(this)}]";
    }
}