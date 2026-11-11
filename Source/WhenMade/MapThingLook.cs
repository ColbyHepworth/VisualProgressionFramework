using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>Gives a thing on a map the look of a faction's age, and redraws it when its look changed.</summary>
    internal static class MapThingLook
    {
        internal static void GiveFactionAge(Thing thing, Faction faction, Map map)
        {
            CompAgeLook look = thing.TryGetComp<CompAgeLook>();
            if (look == null)
            {
                return;
            }
            ThingStyleDef styleBefore = thing.StyleDef;
            look.ApplyLookForFactionAge(faction);
            if (thing.Spawned && thing.StyleDef != styleBefore)
            {
                thing.DirtyMapMesh(map);
            }
        }
    }
}
