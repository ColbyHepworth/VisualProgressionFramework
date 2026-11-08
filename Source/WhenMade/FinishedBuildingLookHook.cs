using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Gives each finished building the look of its builder faction's age. Runs after the game sets the ideology style.
    /// </summary>
    [HarmonyPatch(typeof(Frame), nameof(Frame.CompleteConstruction))]
    public static class FinishedBuildingLookHook
    {
        private const int WarningKey = 0x56504732;

        // The frame is destroyed before the building spawns, so where it stood is noted first.
        private static void Prefix(Frame __instance, out (Map map, IntVec3 cell, ThingDef built, Faction faction) __state)
        {
            __state = (__instance.Map, __instance.Position, __instance.def.entityDefToBuild as ThingDef, __instance.Faction);
        }

        private static void Postfix((Map map, IntVec3 cell, ThingDef built, Faction faction) __state)
        {
            if (__state.map == null || __state.built == null)
            {
                return;
            }
            try
            {
                Thing building = __state.map.thingGrid.ThingAt(__state.cell, __state.built);
                CompAgeLook look = building?.TryGetComp<CompAgeLook>();
                if (look == null)
                {
                    return;
                }
                ThingStyleDef styleBefore = building.StyleDef;
                // The frame carries the look from when its blueprint was placed, and the age may have changed since.
                if (look.HasAgeLook)
                {
                    building.StyleDef = null;
                }
                look.ApplyLookForFactionAge(__state.faction);
                if (building.StyleDef != styleBefore)
                {
                    building.DirtyMapMesh(__state.map);
                }
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Visual Progression] Couldn't give a finished building its look: " + e, WarningKey);
            }
        }
    }
}
