using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Attaches each <see cref="ItemLooksDef"/> to its item and merges every look list an item has into one, so a pack
    /// needs no patch and two packs listing the same item both count. It runs before the game makes its generated defs
    /// and resolves references, so everything after sees one list, as if a single patch had added it.
    /// </summary>
    [HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
    public static class LookListsHook
    {
        // An error escaping here would make the game reset the player's mod list, so nothing may escape.
        private static void Prefix()
        {
            try
            {
                MergeAllLists();
            }
            catch (Exception e)
            {
                Log.Warning("[Visual Progression] Couldn't attach the look lists, so listed items may keep their normal art: " + e);
            }
        }

        private static void MergeAllLists()
        {
            Dictionary<ThingDef, List<ItemLooksDef>> defsByItem = DefDatabase<ItemLooksDef>.AllDefsListForReading
                .Where(def => def.item != null)
                .GroupBy(def => def.item)
                .ToDictionary(group => group.Key, group => group.ToList());
            List<ThingDef> itemsWithSeveralPatchedLists = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(item => item.comps != null && item.comps.OfType<CompProperties_AgeLook>().Count() > 1)
                .ToList();
            foreach (ThingDef item in defsByItem.Keys.Concat(itemsWithSeveralPatchedLists).Distinct().ToList())
            {
                try
                {
                    MergeLists(item, defsByItem.TryGetValue(item, out List<ItemLooksDef> defs) ? defs : new List<ItemLooksDef>());
                }
                catch (Exception e)
                {
                    Log.Warning($"[Visual Progression] Couldn't attach the looks for {item.defName}, so it may keep its normal art: " + e);
                }
            }
        }

        // Patched lists come first, since patches can't say which mod added them. Defs follow in load order.
        private static void MergeLists(ThingDef item, List<ItemLooksDef> defs)
        {
            item.comps ??= new List<CompProperties>();
            List<CompProperties_AgeLook> patched = item.comps.OfType<CompProperties_AgeLook>().ToList();
            IEnumerable<IReadOnlyList<AgeLookRow>> lists = patched.Select(props => props.looks ?? new List<AgeLookRow>())
                .Concat(defs.Select(def => def.looks ?? new List<AgeLookRow>()));
            List<AgeLookRow> merged = LookLists.Merged(lists, row => (int)row.upToAge);

            CompProperties_AgeLook kept = patched.FirstOrDefault();
            if (kept == null)
            {
                item.comps.Add(new CompProperties_AgeLook { looks = merged });
                return;
            }
            kept.looks = merged;
            foreach (CompProperties_AgeLook extra in patched.Skip(1))
            {
                item.comps.Remove(extra);
            }
        }
    }
}
