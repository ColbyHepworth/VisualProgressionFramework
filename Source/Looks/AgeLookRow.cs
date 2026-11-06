using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>One look of an item: the style it's crafted with while the colony's age is at or below upToAge.</summary>
    public class AgeLookRow
    {
        public TechLevel upToAge;
        public ThingStyleDef style;
    }
}
