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
            // The crafting hook goes first and alone, so a failed hook on another mod can't stop it.
            harmony.CreateClassProcessor(typeof(CraftedItemLookHook)).Patch();
            AgeChangeHooks.Apply(harmony);
        }
    }
}
