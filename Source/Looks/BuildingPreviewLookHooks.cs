using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Shows a building with looks in the look it will be built with while it's placed and while its blueprint waits. A
    /// frame draws the game's plain frame, and an ideology style the game picked still wins.
    /// </summary>
    public static class BuildingPreviewLookHooks
    {
        private const int WarningKey = 0x56504737;

        /// <summary>
        /// The build designator's style without an ideology precept. The game uses it for the placing preview, the blueprint
        /// it places, the button's icon and buildings placed in god mode.
        /// </summary>
        [HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.ThingStyleDefNonPreceptSource), MethodType.Getter)]
        public static class PlacingLook
        {
            private static void Postfix(Designator_Build __instance, ref ThingStyleDef __result)
            {
                try
                {
                    CompProperties_AgeLook props = (__instance.PlacingDef as ThingDef)?.GetCompProperties<CompProperties_AgeLook>();
                    TechLevel colonyAge = Faction.OfPlayer.def.techLevel;
                    if (props == null || colonyAge == TechLevel.Undefined)
                    {
                        return;
                    }
                    // A copied building carries the look of the age it was built in, but it's built in the current one.
                    if (__result == null || props.IsAgeLook(__result))
                    {
                        __result = props.LookFor(colonyAge);
                    }
                }
                catch (Exception e)
                {
                    Log.WarningOnce("[Visual Progression] Couldn't give a building's preview its look: " + e, WarningKey);
                }
            }
        }

        /// <summary>
        /// Every build blueprint, including a rebuilt building's, which carries the look of the age the old one was built in,
        /// and blueprints other factions place.
        /// </summary>
        [HarmonyPatch(typeof(GenConstruct), nameof(GenConstruct.PlaceBlueprintForBuild))]
        public static class BlueprintLook
        {
            private static void Postfix(BuildableDef sourceDef, Blueprint_Build __result)
            {
                try
                {
                    CompProperties_AgeLook props = (sourceDef as ThingDef)?.GetCompProperties<CompProperties_AgeLook>();
                    Faction faction = __result?.Faction;
                    if (props == null || faction == null || faction.def.techLevel == TechLevel.Undefined || __result.StyleSourcePrecept != null)
                    {
                        return;
                    }
                    if (__result.StyleDef == null || props.IsAgeLook(__result.StyleDef))
                    {
                        __result.StyleDef = props.LookFor(faction.def.techLevel);
                    }
                }
                catch (Exception e)
                {
                    Log.WarningOnce("[Visual Progression] Couldn't give a blueprint its look: " + e, WarningKey + 1);
                }
            }
        }
    }
}
