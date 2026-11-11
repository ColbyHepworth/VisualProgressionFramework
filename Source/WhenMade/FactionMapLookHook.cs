using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Gives a newly generated map's faction buildings the look of their faction's age, and the items lying on another
    /// faction's map that faction's age. A base, an outpost or a camp then looks like the faction that holds it.
    /// Ruins, which have no faction or a hidden one, and maps no faction owns keep the normal art.
    /// </summary>
    [HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.GenerateMap))]
    public static class FactionMapLookHook
    {
        private const int WarningKey = 0x56504739;

        // Runs after other mods' postfixes, so what they add to the new map gets a look too.
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Map __result)
        {
            if (__result == null)
            {
                return;
            }
            // An error here must not stop the map from being made.
            try
            {
                GiveBuildingsTheirFactionsAge(__result);
                GiveLyingItemsTheOwnersAge(__result);
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Visual Progression] Couldn't give a new map's things their faction's look: " + e, WarningKey);
            }
        }

        private static void GiveBuildingsTheirFactionsAge(Map map)
        {
            foreach (Thing building in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
            {
                if (IsAnotherFaction(building.Faction))
                {
                    MapThingLook.GiveFactionAge(building, building.Faction, map);
                }
            }
        }

        // Things pawns hold are left alone: their gear already has their own faction's look.
        private static void GiveLyingItemsTheOwnersAge(Map map)
        {
            Faction owner = map.ParentFaction;
            if (!IsAnotherFaction(owner))
            {
                return;
            }
            foreach (Thing item in map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver))
            {
                MapThingLook.GiveFactionAge(item.GetInnerIfMinified(), owner, map);
            }
        }

        // Ancient ruins and mech clusters belong to hidden factions, and count as nobody's.
        private static bool IsAnotherFaction(Faction faction)
        {
            return faction != null && !faction.IsPlayer && !faction.Hidden;
        }
    }
}
