using System.Collections.Generic;
using System.Linq;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// An item's looks by age, attached to the item when the game starts (<see cref="LookListsHook"/>). Several of these,
    /// or a patched <see cref="CompProperties_AgeLook"/>, for the same item merge into one list.
    /// </summary>
    public class ItemLooksDef : Def
    {
        public ThingDef item;
        public List<AgeLookRow> looks;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            if (item == null)
            {
                yield return "ItemLooksDef has no item.";
            }
            else if (item.comps == null || !item.comps.Any(comp => comp is CompProperties_AgeLook))
            {
                // The looks are attached before the game makes its generated defs, like meat and corpses.
                yield return $"{item.defName} is made by the game at startup and can't take looks.";
            }
            if (looks.NullOrEmpty())
            {
                yield return "ItemLooksDef has no looks.";
            }
        }
    }
}
