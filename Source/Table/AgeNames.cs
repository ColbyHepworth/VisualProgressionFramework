using System;
using System.Collections.Generic;

namespace VisualProgression
{
    /// <summary>Picks the name an item type shows at an age, and keeps items on the type's normal name.</summary>
    public static class AgeNames
    {
        /// <summary>
        /// The name of the row that applies at the age when that row has one, otherwise the normal name. rowNames[i]
        /// belongs to row i and may be null or empty.
        /// </summary>
        public static string TypeNameFor(IReadOnlyList<int> upToAges, IReadOnlyList<string> rowNames, string normalName, int age)
        {
            int row = AgeLookTable.RowFor(upToAges, age);
            if (row < 0 || string.IsNullOrEmpty(rowNames[row]))
            {
                return normalName;
            }
            return rowNames[row];
        }

        /// <summary>
        /// An item label the game built on the type's swapped name, rebuilt on its normal name. A label that doesn't start
        /// with the swapped name is returned as it is.
        /// </summary>
        public static string WithNormalTypeName(string label, string swappedBase, string normalBase)
        {
            if (string.IsNullOrEmpty(swappedBase) || !label.StartsWith(swappedBase, StringComparison.Ordinal))
            {
                return label;
            }
            return normalBase + label.Substring(swappedBase.Length);
        }
    }
}
