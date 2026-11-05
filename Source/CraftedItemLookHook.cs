using HarmonyLib;
using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>Gives each finished bill product its look. Runs after the game sets the crafter's ideology style.</summary>
    [HarmonyPatch(typeof(GenRecipe), "PostProcessProduct")]
    public static class CraftedItemLookHook
    {
        private static void Postfix(Thing __result)
        {
            __result.GetInnerIfMinified().TryGetComp<CompAgeLook>()?.ApplyLookForColonyAge();
        }
    }
}
