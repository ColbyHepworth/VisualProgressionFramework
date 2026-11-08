using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualProgression
{
    /// <summary>One item's look lists from several mods, merged into one.</summary>
    public static class LookLists
    {
        /// <summary>
        /// The rows of the lists, given in load order. A later list's row replaces an earlier list's rows for the same age.
        /// Rows of one list are all kept, even two for the same age (the item's config check reports those). The order of
        /// the result doesn't matter.
        /// </summary>
        public static List<TRow> Merged<TRow>(IEnumerable<IReadOnlyList<TRow>> listsInLoadOrder, Func<TRow, int> upToAge)
        {
            var merged = new List<TRow>();
            foreach (IReadOnlyList<TRow> list in listsInLoadOrder)
            {
                HashSet<int> agesInList = list.Select(upToAge).ToHashSet();
                merged.RemoveAll(row => agesInList.Contains(upToAge(row)));
                merged.AddRange(list);
            }
            return merged;
        }
    }
}
