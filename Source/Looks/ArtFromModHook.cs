using HarmonyLib;
using UnityEngine;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Gives a look with <see cref="GraphicData_FromMod"/> the named mod's texture. The game looks a path up in the last
    /// loaded mod first, so a retexture at the same path would otherwise win. Stack-count and random graphics build each
    /// of their images as a single graphic with the same graphic data, so this one hook covers them too.
    /// </summary>
    [HarmonyPatch(typeof(Graphic_Single), nameof(Graphic_Single.Init))]
    public static class ArtFromModHook
    {
        private static void Prefix(ref GraphicRequest req)
        {
            if (req.texture != null || !(req.graphicData is GraphicData_FromMod fromModData))
            {
                return;
            }
            // The item's own mod is filled in when references are resolved, and some loaders build graphics before that.
            // The look is built again once it's filled in, and an item without a mod gets its own warning then.
            if (fromModData.fromMod.NullOrEmpty())
            {
                return;
            }
            ModContentPack mod = RunningMod(fromModData.fromMod);
            if (mod == null)
            {
                // Stack and random graphics come here once per image, so this warns once per look.
                string look = fromModData.texPath;
                Log.WarningOnce($"[Visual Progression] The look at {look} wants art from \"{fromModData.fromMod}\", " +
                    "which isn't active, so it shows the normal art.", ("VP_FromMod_" + look).GetHashCode());
                return;
            }
            Texture2D texture = ModOwnTextures.From(mod, req.path, out string problem);
            if (texture == null)
            {
                Log.WarningOnce($"[Visual Progression] {fromModData.fromMod} doesn't give the image at {req.path} ({problem}), so " +
                    "that look shows the normal art.", ("VP_FromMod_" + req.path).GetHashCode());
                return;
            }
            req.texture = texture;
        }

        private static ModContentPack RunningMod(string packageId)
        {
            if (packageId.NullOrEmpty())
            {
                return null;
            }
            // PackageId gains "_steam" when a mod is installed both locally and from the Workshop. The player-facing one doesn't.
            foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
            {
                if (string.Equals(mod.PackageIdPlayerFacing, packageId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return mod;
                }
            }
            return null;
        }
    }
}
