using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>Applies the new age's icons the moment VFE Tribals or Node Research changes the colony's age.</summary>
    public static class AgeChangeHooks
    {
        private static bool tribalsHooked;
        private static bool nodeResearchHooked;

        /// <summary>True when at least one of the hooks is in place, so the hourly check isn't needed.</summary>
        public static bool AnyHooked => tribalsHooked || nodeResearchHooked;

        /// <summary>Hooks each mod that's active. A hook that can't be placed logs a warning and leaves the hourly check on.</summary>
        public static void Apply(Harmony harmony)
        {
            // Every age change in VFE Tribals ends here, and its tick calls it when any other mod raises the age.
            tribalsHooked = TryHook(harmony, "oskarpotocki.vfe.tribals", "VFE Tribals",
                () => AccessTools.Method(AccessTools.TypeByName("VFETribals.GameComponent_Tribals"), "AdvanceToEra"));
            // Node Research opens this window right after changing the age, from its research and from its button.
            nodeResearchHooked = TryHook(harmony, "ferny.noderesearch", "Node Research",
                () => AccessTools.Constructor(AccessTools.TypeByName("BetterResearchMenu.Window_TechAdvance"), new[] { typeof(TechLevel) }));
        }

        private static bool TryHook(Harmony harmony, string packageId, string modName, Func<MethodBase> findTarget)
        {
            if (!ModsConfig.IsActive(packageId))
            {
                return false;
            }
            try
            {
                MethodBase target = findTarget();
                if (target == null)
                {
                    Log.Warning($"[Visual Progression] Couldn't find where {modName} changes the colony's age, so icons check it "
                        + "once an in game hour instead.");
                    return false;
                }
                harmony.Patch(target, postfix: new HarmonyMethod(typeof(AgeChangeHooks), nameof(ApplyColonyAge)));
                return true;
            }
            catch (Exception e)
            {
                Log.Warning($"[Visual Progression] Couldn't hook {modName}'s age change, so icons check it once an in game hour "
                    + "instead: " + e);
                return false;
            }
        }

        private static void ApplyColonyAge()
        {
            AgeTypeLooks.ApplyFor(Faction.OfPlayer.def.techLevel);
        }
    }
}
