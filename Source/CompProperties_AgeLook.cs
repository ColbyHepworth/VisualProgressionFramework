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

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }
            foreach (CompProperties comp in parentDef.comps)
            {
                if (comp != this && typeof(CompStyleable).IsAssignableFrom(comp.compClass))
                {
                    yield return "This item already has a styleable comp, and looks by age don't support that yet.";
                }
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
