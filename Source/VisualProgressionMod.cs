using System;
using HarmonyLib;
using Verse;

namespace VisualProgression
{
    /// <summary>Applies the framework's Harmony patches when the game loads mods.</summary>
    public class VisualProgressionMod : Mod
    {
        public VisualProgressionMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("groovy.visualprogression.framework");
            // The game's own hooks go first, each alone, so a failed hook can't stop the others.
            TryPatch(harmony, typeof(CraftedItemLookHook), "crafted items");
            TryPatch(harmony, typeof(StartingItemsLookHook.WhileMapIsMade), "starting items");
            TryPatch(harmony, typeof(StartingItemsLookHook.AfterMapIsMade), "starting items added after the map");
            TryPatch(harmony, typeof(PawnGearLookHook), "pawns' gear");
            AgeChangeHooks.Apply(harmony);
        }

        private static void TryPatch(Harmony harmony, Type hook, string whatGetsLooks)
        {
            try
            {
                harmony.CreateClassProcessor(hook).Patch();
            }
            catch (Exception e)
            {
                Log.Warning($"[Visual Progression] Couldn't hook the game, so {whatGetsLooks} keep their normal art: " + e);
            }
        }
    }
}
