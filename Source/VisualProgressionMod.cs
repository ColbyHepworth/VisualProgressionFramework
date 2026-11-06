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
            harmony.CreateClassProcessor(typeof(CraftedItemLookHook)).Patch();
            harmony.CreateClassProcessor(typeof(StartingItemsLookHook.WhileMapIsMade)).Patch();
            harmony.CreateClassProcessor(typeof(StartingItemsLookHook.AfterMapIsMade)).Patch();
            AgeChangeHooks.Apply(harmony);
        }
    }
}
