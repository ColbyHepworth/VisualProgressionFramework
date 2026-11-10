using Verse;

namespace VisualProgression
{
    /// <summary>
    /// A look's graphic data whose art comes from one mod's files, so a retexture of the same path loaded after that mod
    /// doesn't replace it. Works for single, stack-count, random and four-facing graphics, masks included.
    /// </summary>
    public class GraphicData_FromMod : GraphicData
    {
        /// <summary>The packageId of the mod whose art the look draws, such as Ludeon.RimWorld for the base game.</summary>
        public string fromMod;
    }
}
