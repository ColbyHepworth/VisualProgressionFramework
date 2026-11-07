using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Keeps an item's own name when its type's name follows the colony's age. The game names an item after its type
    /// unless its style gives a name, so an item without a named look would otherwise take the current age's name.
    /// </summary>
    [HarmonyPatch(typeof(GenLabel), "NewThingLabel", new[] { typeof(Thing), typeof(int), typeof(bool), typeof(bool) })]
    public static class ItemNameHook
    {
        private const int WarningKey = 0x56504736;

        private static void Postfix(Thing t, ref string __result)
        {
            try
            {
                string normalName = AgeTypeLooks.NormalNameWhileSwapped(t.def);
                if (normalName == null || !(t.StyleDef?.overrideLabel).NullOrEmpty())
                {
                    return;
                }
                string swappedBase = GenLabel.ThingLabel(t.def, t.Stuff);
                string normalBase = t.Stuff != null ? (string)"ThingMadeOfStuffLabel".Translate(t.Stuff.LabelAsStuff, normalName) : normalName;
                __result = AgeNames.WithNormalTypeName(__result, swappedBase, normalBase);
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Visual Progression] Couldn't give an item its own name: " + e, WarningKey);
            }
        }
    }
}
