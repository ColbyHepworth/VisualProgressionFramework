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
            TryPatch(harmony, typeof(CraftedItemLookHook), "crafted items keep their normal art");
            TryPatch(harmony, typeof(StartingItemsLookHook.WhileMapIsMade), "starting items keep their normal art");
            TryPatch(harmony, typeof(StartingItemsLookHook.AfterMapIsMade), "starting items added after the map keep their normal art");
            TryPatch(harmony, typeof(PawnGearLookHook), "pawns' gear keeps its normal art");
            TryPatch(harmony, typeof(FinishedBuildingLookHook), "finished buildings keep their normal art");
            TryPatch(harmony, typeof(SurgeryProductLookHook.MadeItem), "new items never get a look as they're made, so surgery products keep their normal art");
            TryPatch(harmony, typeof(SurgeryProductLookHook.DuringSurgery), "surgeries aren't watched, so their products keep their normal art");            TryPatch(harmony, typeof(StyleDominanceHook), "Ideology counts things with a look as another ideology's style");
            TryPatch(harmony, typeof(ItemNameHook), "items without a named look show the type's current-age name");
            TryPatch(harmony, typeof(ArtFromModHook), "looks that name a mod's art show the normal art");
            TryPatch(harmony, typeof(ItemButtonIconHooks.DrugButton), "the take-drug button shows the type's icon");
            TryPatch(harmony, typeof(ItemButtonIconHooks.UseButton), "items' use buttons show the type's icon");
            TryPatch(harmony, typeof(ItemButtonIconHooks.ApparelAbilityButton), "apparel ability buttons show the type's icon");
            AgeChangeHooks.Apply(harmony);
        }

        private static void TryPatch(Harmony harmony, Type hook, string whatHappensInstead)
        {
            try
            {
                harmony.CreateClassProcessor(hook).Patch();
            }
            catch (Exception e)
            {
                Log.Warning($"[Visual Progression] Couldn't hook the game, so {whatHappensInstead}: " + e);
            }
        }
    }
}
