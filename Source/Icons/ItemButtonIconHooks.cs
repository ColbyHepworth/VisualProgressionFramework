using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Gives the buttons for a real item with looks that item's own icon. The game draws them with the item type's icon,
    /// which follows the colony's age instead.
    /// </summary>
    public static class ItemButtonIconHooks
    {
        private const int WarningKey = 0x56504734;
        private static readonly Vector2 ButtonIconSize = new Vector2(75f, 75f);

        [HarmonyPatch(typeof(Pawn_InventoryTracker), nameof(Pawn_InventoryTracker.GetGizmos))]
        public static class DrugButton
        {
            // The game makes this button only when exactly one drug can be taken, so it matches the one item it shows.
            private static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Pawn_InventoryTracker __instance)
            {
                foreach (Gizmo gizmo in gizmos)
                {
                    if (gizmo is Command_Action command)
                    {
                        foreach (Thing item in __instance.innerContainer)
                        {
                            if (command.icon == item.def.uiIcon && TryGiveOwnIcon(command, item))
                            {
                                break;
                            }
                        }
                    }
                    yield return gizmo;
                }
            }
        }

        [HarmonyPatch(typeof(CompUsable), nameof(CompUsable.CompGetGizmosExtra))]
        public static class UseButton
        {
            private static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, CompUsable __instance)
            {
                foreach (Gizmo gizmo in gizmos)
                {
                    if (gizmo is Command command && command.icon == __instance.parent.def.uiIcon)
                    {
                        TryGiveOwnIcon(command, __instance.parent);
                    }
                    yield return gizmo;
                }
            }
        }

        [HarmonyPatch(typeof(CompApparelVerbOwner), "CreateVerbTargetCommand")]
        public static class ApparelAbilityButton
        {
            private static void Postfix(Command_VerbTarget __result, Thing gear)
            {
                if (__result != null && __result.icon == gear.def.uiIcon)
                {
                    TryGiveOwnIcon(__result, gear);
                }
            }
        }

        /// <summary>The item's own icon when it has looks, or null.</summary>
        internal static Texture OwnIcon(Thing item)
        {
            if (item.TryGetComp<CompAgeLook>() == null)
            {
                return null;
            }
            return Widgets.GetIconFor(item, ButtonIconSize, null, false, out _, out _, out _, out _, out _);
        }

        // Buttons are made every frame while something is selected, so an error here is logged once and the button keeps
        // the game's icon.
        private static bool TryGiveOwnIcon(Command command, Thing item)
        {
            try
            {
                Texture icon = OwnIcon(item);
                if (icon == null)
                {
                    return false;
                }
                command.icon = icon;
                return true;
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Visual Progression] Couldn't give a button its item's look: " + e, WarningKey);
                return false;
            }
        }
    }
}
