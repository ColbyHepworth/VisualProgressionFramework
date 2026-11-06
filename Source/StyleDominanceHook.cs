using HarmonyLib;
using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Counts a thing with an age look like one with no style when Ideology weighs a room's styles. Otherwise furniture
    /// with a look would count as another ideology's style and upset colonists who care about style.
    /// </summary>
    [HarmonyPatch(typeof(IdeoUtility), "GetStyleDominance")]
    public static class StyleDominanceHook
    {
        private static void Postfix(Thing t, ref float __result)
        {
            if (__result < 0f && t.TryGetComp<CompAgeLook>()?.HasAgeLook == true)
            {
                __result = 0f;
            }
        }
    }
}
