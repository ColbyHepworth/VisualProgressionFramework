using System;
using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>The debug actions (Debug actions menu, category "Visual Progression"), for testing an item's looks.</summary>
    public static class VisualProgressionDebugActions
    {
        [DebugAction("Visual Progression", "Set colony tech level...", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SetColonyTechLevel()
        {
            var levels = new List<DebugMenuOption>();
            foreach (TechLevel level in Enum.GetValues(typeof(TechLevel)))
            {
                if (level != TechLevel.Undefined)
                {
                    levels.Add(new DebugMenuOption(level.ToString(), DebugMenuOptionMode.Action,
                        () => Faction.OfPlayer.def.techLevel = level));
                }
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(levels));
        }
    }
}
