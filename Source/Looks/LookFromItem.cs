using System;
using HarmonyLib;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Fills in what a look leaves to its item: the item's graphic class, and for art from a mod, the item's own mod.
    /// </summary>
    public static class LookFromItem
    {
        private static readonly AccessTools.FieldRef<GraphicData, Graphic> cachedGraphic =
            AccessTools.FieldRefAccess<GraphicData, Graphic>("cachedGraphic");

        /// <summary>Fills in the look's missing graphic class and mod from the item, and rebuilds its graphic if it changed.</summary>
        public static void FillIn(ThingStyleDef look, ThingDef item)
        {
            GraphicData lookGraphic = look.graphicData;
            if (lookGraphic == null)
            {
                return;
            }
            Type itemGraphicClass = item.graphicData?.graphicClass;
            bool fillClass = lookGraphic.graphicClass == null && itemGraphicClass != null;
            string itemsMod = null;
            if (lookGraphic is GraphicData_FromMod fromModData && fromModData.fromMod.NullOrEmpty() && fromModData.skipMods.NullOrEmpty())
            {
                itemsMod = item.modContentPack?.PackageIdPlayerFacing;
                if (itemsMod == null)
                {
                    // The game only knows the mod of a def read from a mod's Defs files, not one a patch adds.
                    Log.WarningOnce($"[Visual Progression] {item.defName} was added by a patch, so it has no mod of its own. "
                        + $"The look {look.defName} needs a fromMod, and shows the normal art until it has one.",
                        ("VP_NoItemMod_" + look.defName).GetHashCode());
                }
            }
            if (!fillClass && itemsMod == null)
            {
                return;
            }

            // Some mod loaders build looks' graphics before references are resolved, and the game caches a graphic by its
            // graphic data. So the filled-in look gets a copy of its graphic data, and its graphic and icon are built again.
            GraphicData filled = (GraphicData)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(lookGraphic, null);
            cachedGraphic(filled) = null;
            if (fillClass)
            {
                filled.graphicClass = itemGraphicClass;
            }
            if (itemsMod != null)
            {
                ((GraphicData_FromMod)filled).fromMod = itemsMod;
            }
            look.graphicData = filled;
            look.PostLoad();
        }
    }
}
