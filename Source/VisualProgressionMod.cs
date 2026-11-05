using HarmonyLib;
using Verse;

namespace VisualProgression
{
    /// <summary>Applies the framework's Harmony patch when the game loads mods.</summary>
    public class VisualProgressionMod : Mod
    {
        public VisualProgressionMod(ModContentPack content) : base(content)
        {
            new Harmony("groovy.visualprogression.framework").PatchAll();
        }
    }
}
