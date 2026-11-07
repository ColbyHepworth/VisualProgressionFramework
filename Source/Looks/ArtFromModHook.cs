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
            ModContentPack mod = RunningMod(fromModData.fromMod);
            if (mod == null)
            {
                // Stack and random graphics come here once per image, so this warns once per look.
                string look = fromModData.texPath;
                Log.WarningOnce($"[Visual Progression] The look at {look} wants art from \"{fromModData.fromMod}\", " +
                    "which isn't active, so it shows the normal art.", ("VP_FromMod_" + look).GetHashCode());
                return;
            }
            Texture2D texture = TextureIn(mod, req.path);
            if (texture == null)
            {
                Log.WarningOnce($"[Visual Progression] {fromModData.fromMod} has no texture at {req.path}, so that look " +
                    "shows the normal art.", ("VP_FromMod_" + req.path).GetHashCode());
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

        // The base game keeps its own art in its built-in resources rather than in its mod files.
        private static Texture2D TextureIn(ModContentPack mod, string path)
        {
            Texture2D texture = mod.GetContentHolder<Texture2D>().Get(path);
            if (texture == null && mod.IsCoreMod)
            {
                texture = Resources.Load<Texture2D>(GenFilePaths.ContentPath<Texture2D>() + path);
            }
            return texture;
        }
    }
}
