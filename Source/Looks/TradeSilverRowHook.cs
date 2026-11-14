using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Keeps all silver in one row of a trade, whatever looks it has. A trade puts things that can't stack in separate
    /// rows, and only the first silver row counts as money: the others are hidden and can't pay. Silver with different
    /// looks doesn't stack, so without this the trader seems to have no silver, or part of the colony's can't be spent.
    /// </summary>
    [HarmonyPatch(typeof(TransferableUtility), nameof(TransferableUtility.TradeableMatching))]
    public static class TradeSilverRowHook
    {
        private static void Postfix(Thing thing, List<Tradeable> tradeables, ref Tradeable __result)
        {
            if (__result != null || thing == null || tradeables == null || thing.def != ThingDefOf.Silver)
            {
                return;
            }
            foreach (Tradeable row in tradeables)
            {
                // The game's own test for the row that is money (TradeDeal.CurrencyTradeable).
                if (row != null && row.HasAnyThing && row.ThingDef == ThingDefOf.Silver)
                {
                    __result = row;
                    return;
                }
            }
        }
    }
}
