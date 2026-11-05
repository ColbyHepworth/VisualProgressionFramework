using System.Collections.Generic;

namespace VisualProgression
{
    /// <summary>Picks which of an item's looks applies at an age. Ages are tech levels as numbers.</summary>
    public static class AgeLookTable
    {
        /// <summary>
        /// The index of the row with the lowest upToAge that covers colonyAge, the first such row on a tie, or -1 when
        /// no row covers it and the item keeps its normal art.
        /// </summary>
        public static int RowFor(IReadOnlyList<int> upToAges, int colonyAge)
        {
            int bestRow = -1;
            for (int row = 0; row < upToAges.Count; row++)
            {
                bool covers = upToAges[row] >= colonyAge;
                if (covers && (bestRow < 0 || upToAges[row] < upToAges[bestRow]))
                {
                    bestRow = row;
                }
            }
            return bestRow;
        }
    }
}
