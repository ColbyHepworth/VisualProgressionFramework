using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Gives the items and buildings a new game starts with the look of the colony's starting age, or another faction's
    /// buildings that faction's. Scenario parts add them while the starting map's contents are made
    /// (<see cref="Scenario.GenerateIntoMap"/>) or right after (<see cref="Scenario.PostMapGenerate"/>), and only while a
    /// new game starts.
    /// </summary>
    public static class StartingItemsLookHook
    {
        [HarmonyPatch(typeof(Scenario), nameof(Scenario.GenerateIntoMap))]
        public static class WhileMapIsMade
        {
            private static void Prefix(Map map, out HashSet<Thing> __state) => __state = ItemsBefore(map);

            private static void Postfix(Map map, HashSet<Thing> __state) => ApplyToNewItems(map, __state);
        }

        [HarmonyPatch(typeof(Scenario), nameof(Scenario.PostMapGenerate))]
        public static class AfterMapIsMade
        {
            private static void Prefix(Map map, out HashSet<Thing> __state) => __state = ItemsBefore(map);

            private static void Postfix(Map map, HashSet<Thing> __state) => ApplyToNewItems(map, __state);
        }

        // An error here must not stop the scenario: the game would start without its colonists.
        private static HashSet<Thing> ItemsBefore(Map map)
        {
            if (Find.GameInitData == null)
            {
                return null;
            }
            try
            {
                return new HashSet<Thing>(ItemsWithLooks(map));
            }
            catch (Exception e)
            {
                Log.Warning("[Visual Progression] Couldn't read the starting map's items, so starting items keep their normal look: " + e);
                return null;
            }
        }

        private static void ApplyToNewItems(Map map, HashSet<Thing> itemsBefore)
        {
            if (itemsBefore == null)
            {
                return;
            }
            try
            {
                foreach (Thing item in ItemsWithLooks(map).Where(item => !itemsBefore.Contains(item)))
                {
                    CompAgeLook look = item.TryGetComp<CompAgeLook>();
                    ThingStyleDef styleBefore = item.StyleDef;
                    // Another faction's buildings, like an enemy castle a scenario builds, take that faction's age.
                    look.ApplyLookForFactionAge(item.Faction != null && !item.Faction.IsPlayer ? item.Faction : Faction.OfPlayer);
                    if (item.Spawned && item.StyleDef != styleBefore)
                    {
                        item.DirtyMapMesh(map);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("[Visual Progression] Couldn't give starting items their look: " + e);
            }
        }

        // Includes items still in drop pods and colonists' inventories, furniture packed for moving, and buildings.
        private static List<Thing> ItemsWithLooks(Map map)
        {
            var items = new List<Thing>();
            ThingOwnerUtility.GetAllThingsRecursively(map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), items);
            items = items.Select(item => item.GetInnerIfMinified()).ToList();
            items.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial));
            return items.Where(item => item.TryGetComp<CompAgeLook>() != null).ToList();
        }
    }
}
