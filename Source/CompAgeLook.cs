using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Gives an item the look of the age it was made in, held in the item's style slot, and keeps that look through
    /// stacking and splitting.
    /// </summary>
    public class CompAgeLook : ThingComp
    {
        private CompProperties_AgeLook Props => (CompProperties_AgeLook)props;

        /// <summary>Sets the look for the colony's current age, unless the item already has a style.</summary>
        public void ApplyLookForColonyAge()
        {
            ApplyLookForFactionAge(Faction.OfPlayer);
        }

        /// <summary>
        /// Sets the look for the faction's age, unless the item already has a style. With no faction, or a faction with no
        /// tech level, it keeps its normal art.
        /// </summary>
        public void ApplyLookForFactionAge(Faction faction)
        {
            List<AgeLookRow> looks = Props.looks;
            if (faction == null || faction.def.techLevel == TechLevel.Undefined || parent.StyleDef != null || parent.StyleSourcePrecept != null || looks.NullOrEmpty())
            {
                return;
            }
            List<int> upToAges = looks.Select(row => (int)row.upToAge).ToList();
            int rowIndex = AgeLookTable.RowFor(upToAges, (int)faction.def.techLevel);
            if (rowIndex >= 0)
            {
                parent.StyleDef = looks[rowIndex].style;
            }
        }

        /// <summary>Whether the item's style is one of its own age looks, not an ideology's.</summary>
        public bool HasAgeLook => parent.StyleDef != null && Props.looks != null && Props.looks.Any(row => row.style == parent.StyleDef);

        /// <summary>
        /// Trader goods get the selling faction's age. The item's style slot gives the faction's ideology style, which wins
        /// whichever runs first.
        /// </summary>
        public override void PostPostGeneratedForTrader(TraderKindDef trader, PlanetTile forTile, Faction forFaction)
        {
            ApplyLookForFactionAge(forFaction);
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
