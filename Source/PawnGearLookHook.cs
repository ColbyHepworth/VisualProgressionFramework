using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Gives a generated pawn's apparel, weapons and inventory the look of its faction's age. Pawns with no faction keep
    /// the normal art.
    /// </summary>
    [HarmonyPatch(typeof(PawnGenerator), "GenerateGearFor")]
    public static class PawnGearLookHook
    {
        private const int WarningKey = 0x56504731;

        // Runs after other mods gear postfixes, so the items they add get a look too.
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Pawn pawn)
        {
            try
            {
                foreach (Thing item in Gear(pawn))
                {
                    item.TryGetComp<CompAgeLook>()?.ApplyLookForFactionAge(pawn.Faction);
                }
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Visual Progression] Couldn't give a new pawn's gear its look: " + e, WarningKey);
            }
        }

        private static IEnumerable<Thing> Gear(Pawn pawn)
        {
            var gear = new List<Thing>();
            if (pawn.apparel != null)
            {
                gear.AddRange(pawn.apparel.WornApparel);
            }
            if (pawn.equipment != null)
            {
                gear.AddRange(pawn.equipment.AllEquipmentListForReading);
            }
            if (pawn.inventory != null)
            {
                gear.AddRange(pawn.inventory.innerContainer);
            }
            return gear;
        }
    }
}
