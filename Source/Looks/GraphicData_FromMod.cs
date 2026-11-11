using System.Collections.Generic;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Graphic data whose art comes from a chosen mod's files: one named mod, so a retexture of the same path loaded after
    /// it doesn't replace it, or whichever mod the game would pick with some mods passed over, so a pack's own retextures
    /// stay out of the ages it leaves to other retextures. Works for single, stack-count, random and four-facing graphics,
    /// masks included.
    /// </summary>
    public class GraphicData_FromMod : GraphicData
    {
        /// <summary>The packageId of the mod whose art the look draws, such as Ludeon.RimWorld for the base game.</summary>
        public string fromMod;

        /// <summary>Mods to pass over: the art comes from whichever other mod the game would pick at the path.</summary>
        public List<string> skipMods;

        /// <summary>A new graphic data that takes its art from the same mod, or skips the same mods, as this one.</summary>
        public GraphicData_FromMod CopyArtSource()
        {
            return new GraphicData_FromMod
            {
                fromMod = fromMod,
                skipMods = skipMods == null ? null : new List<string>(skipMods),
            };
        }

        /// <summary>True when the art comes from whichever mod wins, with some mods passed over.</summary>
        public bool SkipsMods => fromMod.NullOrEmpty() && !skipMods.NullOrEmpty();
    }
}
