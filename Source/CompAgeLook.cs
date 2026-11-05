using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>Gives an item the look of the age it was crafted in and keeps that look through stacking and splitting.</summary>
    public class CompAgeLook : CompStyleable
    {
        private CompProperties_AgeLook Props => (CompProperties_AgeLook)props;

        /// <summary>Sets the look for the colony's current age, unless the item already has a style.</summary>
        public void ApplyLookForColonyAge()
        {
            List<AgeLookRow> looks = Props.looks;
            if (parent.StyleDef != null || parent.StyleSourcePrecept != null || looks.NullOrEmpty())
            {
                return;
            }
            List<int> upToAges = looks.Select(row => (int)row.upToAge).ToList();
            int rowIndex = AgeLookTable.RowFor(upToAges, (int)Faction.OfPlayer.def.techLevel);
            if (rowIndex >= 0)
            {
                parent.StyleDef = looks[rowIndex].style;
            }
        }

        public override bool AllowStackWith(Thing other)
        {
            return base.AllowStackWith(other) && other.StyleDef == parent.StyleDef;
        }

        public override void PostSplitOff(Thing piece)
        {
            base.PostSplitOff(piece);
            piece.StyleDef = parent.StyleDef;
        }
    }
}
