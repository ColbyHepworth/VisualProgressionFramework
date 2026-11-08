using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>Lists an item's looks by age. Past the last row, the item keeps its normal art.</summary>
    public class CompProperties_AgeLook : CompProperties
    {
        public List<AgeLookRow> looks;

        public CompProperties_AgeLook()
        {
            compClass = typeof(CompAgeLook);
        }

        /// <summary>
        /// Adds the game's style slot to the item when it has none, so every listed item has exactly one. A look that
        /// leaves out its graphic class gets the item's, and one that wants a mod's art without naming the mod gets the
        /// item's own mod.
        /// </summary>
        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            if (!parentDef.comps.Any(comp => typeof(CompStyleable).IsAssignableFrom(comp.compClass)))
            {
                parentDef.comps.Add(new CompProperties_Styleable());
            }
            if (looks == null)
            {
                return;
            }
            foreach (AgeLookRow row in looks)
            {
                if (row.style != null)
                {
                    LookFromItem.FillIn(row.style, parentDef);
                }
            }
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }
            if (parentDef.comps.First(comp => comp is CompProperties_AgeLook) != this)
            {
                yield return "This item already has looks from another list, so these are ignored. Put all its looks in one list.";
            }
            if (looks.NullOrEmpty())
            {
                yield return "CompProperties_AgeLook has no looks.";
                yield break;
            }
            foreach (AgeLookRow row in looks)
            {
                if (row.style == null)
                {
                    yield return $"The look up to {row.upToAge} has no style.";
                }
                if (row.upToAge == TechLevel.Undefined)
                {
                    yield return "A look has no upToAge.";
                }
            }
            if (looks.Select(row => row.upToAge).Distinct().Count() < looks.Count)
            {
                yield return "Two looks have the same upToAge.";
            }
        }
    }
}
