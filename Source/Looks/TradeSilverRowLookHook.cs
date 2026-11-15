using HarmonyLib;
using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Shows the trader's silver on a trade's silver row, so the row follows the age of whoever the colony trades with.
    /// The game draws a row's picture, info button and name from the colony's first thing, and only from the trader's
    /// when the colony has none. While the row is drawn, the trader's silver is listed first among the colony's things,
    /// then taken out again, so counts, prices and what a deal moves never see it.
    /// </summary>
    [HarmonyPatch(typeof(TransferableUIUtility), nameof(TransferableUIUtility.DrawTransferableInfo))]
    public static class TradeSilverRowLookHook
    {
        private static void Prefix(Transferable trad, out Thing __state)
        {
            __state = null;
            if (!(trad is Tradeable row) || !TradeSession.Active || !row.HasAnyThing || row.ThingDef != ThingDefOf.Silver)
            {
                return;
            }
            CompProperties_AgeLook silverLooks = ThingDefOf.Silver.GetCompProperties<CompProperties_AgeLook>();
            if (silverLooks == null)
            {
                return;
            }
            Thing tradersSilver = TradersSilver(row, silverLooks.LookFor(TradeSession.trader.Faction));
            if (tradersSilver == null || tradersSilver == row.AnyThing)
            {
                return;
            }
            row.thingsColony.Insert(0, tradersSilver);
            __state = tradersSilver;
        }

        // A finalizer, so the row is put back even when drawing it throws.
        private static void Finalizer(Transferable trad, Thing __state)
        {
            if (__state != null && trad is Tradeable row && row.thingsColony.Count > 0 && row.thingsColony[0] == __state)
            {
                row.thingsColony.RemoveAt(0);
            }
        }

        // The trader's silver with the look their age gives. A trader also holds silver the colony paid earlier in the
        // visit, in whatever look it had, so the first stack isn't always their own. Null when the trader has none.
        private static Thing TradersSilver(Tradeable row, ThingStyleDef tradersLook)
        {
            foreach (Thing silver in row.thingsTrader)
            {
                if (silver.StyleDef == tradersLook)
                {
                    return silver;
                }
            }
            return row.FirstThingTrader;
        }
    }
}
