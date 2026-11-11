using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Gives a look with <see cref="GraphicData_FromMod"/> the chosen mod's pictures and masks. The game looks a path up in
    /// the last loaded mod first, so a retexture at the same path would otherwise win, even one the look skips.
    /// </summary>
    public static class ArtFromModHooks
    {
        /// <summary>
        /// Fills a single picture's request from the chosen mod, which <c>Init</c> uses instead of looking the path up.
        /// Stack-count and random graphics build each of their images as a single graphic with the same graphic data, so
        /// this hook covers them too.
        /// </summary>
        [HarmonyPatch(typeof(Graphic_Single), nameof(Graphic_Single.Init))]
        public static class SinglePicture
        {
            private static void Prefix(ref GraphicRequest req)
            {
                if (req.texture != null || !(req.graphicData is GraphicData_FromMod fromModData))
                {
                    return;
                }
                string path = req.path;
                ModContentPack mod = ChosenMod(fromModData, path, candidate => ModOwnTextures.From(candidate, path, out _) != null);
                if (mod == null)
                {
                    return;
                }
                Texture2D texture = ModOwnTextures.From(mod, req.path, out string problem);
                if (texture == null)
                {
                    Log.WarningOnce($"[Visual Progression] {SourceOf(fromModData)} doesn't give the image at {req.path} ({problem}), so " +
                        "that look shows the normal art.", ("VP_FromModNoArt_" + SourceOf(fromModData) + req.path).GetHashCode());
                    return;
                }
                req.texture = texture;
            }
        }

        /// <summary>
        /// Gives a single picture the chosen mod's mask, which decides where a material's colour goes. Without one there,
        /// the picture has no mask, as in that mod, unless the look names a mask path of its own, which is then found as
        /// usual.
        /// </summary>
        [HarmonyPatch(typeof(Graphic_Single), nameof(Graphic_Single.Init))]
        public static class SingleMask
        {
            private static readonly AccessTools.FieldRef<Graphic_Single, Material> mat =
                AccessTools.FieldRefAccess<Graphic_Single, Material>("mat");

            private static void Postfix(Graphic_Single __instance, GraphicRequest req)
            {
                if (!(req.graphicData is GraphicData_FromMod fromModData) || !req.shader.SupportsMaskTex())
                {
                    return;
                }
                try
                {
                    string path = req.path;
                    ModContentPack mod = ChosenMod(fromModData, path, candidate => ModOwnTextures.From(candidate, path, out _) != null);
                    // A picture the mod doesn't have kept the normal art, so it keeps the normal mask too.
                    if (mod == null || ModOwnTextures.From(mod, req.path, out _) == null)
                    {
                        return;
                    }
                    bool ownMaskPath = !req.maskPath.NullOrEmpty();
                    Texture2D mask = ModOwnTextures.From(mod, ownMaskPath ? req.maskPath : req.path + Graphic_Single.MaskSuffix, out _);
                    // A pack can ship its own mask for another mod's picture, which the game already found.
                    if (mask == null && ownMaskPath)
                    {
                        return;
                    }
                    MaterialRequest request = new MaterialRequest((Texture2D)mat(__instance).mainTexture, req.shader, req.color)
                    {
                        colorTwo = req.colorTwo,
                        renderQueue = req.renderQueue,
                        shaderParameters = req.shaderParameters,
                        maskTex = mask,
                    };
                    mat(__instance) = MaterialPool.MatFrom(request);
                }
                catch (Exception e)
                {
                    Log.WarningOnce($"[Visual Progression] Couldn't give the look at {req.path} its mask from {SourceOf(fromModData)}, so " +
                        "it takes the normal mask: " + e, ("VP_FromModMask_" + req.path).GetHashCode());
                }
            }
        }

        /// <summary>
        /// Gives four-facing art (most buildings and furniture) the chosen mod's pictures and masks. The game reads each
        /// facing by path, so its result is replaced afterwards rather than steered.
        /// </summary>
        [HarmonyPatch(typeof(Graphic_Multi), nameof(Graphic_Multi.Init))]
        public static class FacingPictures
        {
            // Facings in the order Graphic_Multi keeps its materials: north, east, south, west.
            private const int North = 0, East = 1, South = 2, West = 3;

            private static readonly string[] Suffixes =
            {
                Graphic_Multi.NorthSuffix, Graphic_Multi.EastSuffix, Graphic_Multi.SouthSuffix, Graphic_Multi.WestSuffix,
            };

            private static readonly AccessTools.FieldRef<Graphic_Multi, Material[]> mats =
                AccessTools.FieldRefAccess<Graphic_Multi, Material[]>("mats");
            private static readonly AccessTools.FieldRef<Graphic_Multi, bool> eastFlipped =
                AccessTools.FieldRefAccess<Graphic_Multi, bool>("eastFlipped");
            private static readonly AccessTools.FieldRef<Graphic_Multi, bool> westFlipped =
                AccessTools.FieldRefAccess<Graphic_Multi, bool>("westFlipped");
            private static readonly AccessTools.FieldRef<Graphic_Multi, float> drawRotatedExtraAngleOffset =
                AccessTools.FieldRefAccess<Graphic_Multi, float>("drawRotatedExtraAngleOffset");

            private static void Postfix(Graphic_Multi __instance, GraphicRequest req)
            {
                if (!(req.graphicData is GraphicData_FromMod fromModData))
                {
                    return;
                }
                try
                {
                    string path = req.path;
                    ModContentPack mod = ChosenMod(fromModData, path, candidate => HasFacingArt(candidate, path));
                    if (mod == null)
                    {
                        return;
                    }
                    Texture2D[] pictures = new Texture2D[4];
                    for (int facing = North; facing <= West; facing++)
                    {
                        pictures[facing] = ModOwnTextures.From(mod, req.path + Suffixes[facing], out _);
                    }
                    // Art drawn for one facing only is turned to stand in for north, as the game does.
                    float extraAngle = 0f;
                    if (pictures[North] == null)
                    {
                        if (pictures[South] != null)
                        {
                            pictures[North] = pictures[South];
                            extraAngle = 180f;
                        }
                        else if (pictures[East] != null)
                        {
                            pictures[North] = pictures[East];
                            extraAngle = -90f;
                        }
                        else if (pictures[West] != null)
                        {
                            pictures[North] = pictures[West];
                            extraAngle = 90f;
                        }
                        else
                        {
                            pictures[North] = ModOwnTextures.From(mod, req.path, out _);
                        }
                    }
                    if (pictures[North] == null)
                    {
                        Log.WarningOnce($"[Visual Progression] {SourceOf(fromModData)} has no facing images at {req.path}, so " +
                            "that look shows the normal art.", ("VP_FromModNoArt_" + SourceOf(fromModData) + req.path).GetHashCode());
                        return;
                    }
                    bool allowsFlip = req.graphicData.allowFlip;
                    bool eastIsFlippedWest = false;
                    bool westIsFlippedEast = false;
                    if (pictures[South] == null)
                    {
                        pictures[South] = pictures[North];
                    }
                    if (pictures[East] == null)
                    {
                        eastIsFlippedWest = pictures[West] != null && allowsFlip;
                        pictures[East] = pictures[West] ?? pictures[North];
                    }
                    if (pictures[West] == null)
                    {
                        westIsFlippedEast = allowsFlip;
                        pictures[West] = pictures[East];
                    }

                    Texture2D[] masks = new Texture2D[4];
                    if (req.shader.SupportsMaskTex())
                    {
                        // A mask path of its own names the facings directly; otherwise each facing's mask ends in "m".
                        bool ownMaskPath = !req.maskPath.NullOrEmpty();
                        string maskBase = ownMaskPath ? req.maskPath : req.path;
                        string maskEnd = ownMaskPath ? "" : "m";
                        for (int facing = North; facing <= West; facing++)
                        {
                            masks[facing] = ModOwnTextures.From(mod, maskBase + Suffixes[facing] + maskEnd, out _);
                        }
                        masks[North] = masks[North] ?? masks[South] ?? masks[East] ?? masks[West];
                        masks[South] = masks[South] ?? masks[North];
                        masks[East] = masks[East] ?? masks[West] ?? masks[North];
                        masks[West] = masks[West] ?? masks[East];
                        // A pack can ship its own masks for another mod's pictures, which the game already found.
                        if (masks[North] == null && ownMaskPath)
                        {
                            for (int facing = North; facing <= West; facing++)
                            {
                                masks[facing] = GameMask(mats(__instance)[facing]);
                            }
                        }
                    }

                    Material[] materials = mats(__instance);
                    for (int facing = North; facing <= West; facing++)
                    {
                        materials[facing] = MaterialPool.MatFrom(new MaterialRequest
                        {
                            mainTex = pictures[facing],
                            shader = req.shader,
                            color = req.color,
                            colorTwo = req.colorTwo,
                            maskTex = masks[facing],
                            shaderParameters = req.shaderParameters,
                            renderQueue = req.renderQueue,
                        });
                    }
                    eastFlipped(__instance) = eastIsFlippedWest;
                    westFlipped(__instance) = westIsFlippedEast;
                    drawRotatedExtraAngleOffset(__instance) = extraAngle;
                }
                catch (Exception e)
                {
                    Log.WarningOnce($"[Visual Progression] Couldn't give the look at {req.path} its art from {SourceOf(fromModData)}, so " +
                        "it shows the normal art: " + e, ("VP_FromModFacings_" + req.path).GetHashCode());
                }
            }

            // Any facing's image, or the one image drawn for every facing.
            private static bool HasFacingArt(ModContentPack mod, string path)
            {
                foreach (string suffix in Suffixes)
                {
                    if (ModOwnTextures.From(mod, path + suffix, out _) != null)
                    {
                        return true;
                    }
                }
                return ModOwnTextures.From(mod, path, out _) != null;
            }

            private static Texture2D GameMask(Material material)
            {
                return material != null && material.HasProperty(ShaderPropertyIDs.MaskTex)
                    ? material.GetTexture(ShaderPropertyIDs.MaskTex) as Texture2D
                    : null;
            }
        }

        /// <summary>
        /// Keeps the placing preview (the ghost) of a look on the chosen mod's art. The game builds the ghost from a plain
        /// copy of the look's graphic data, so it's built again from a copy that keeps the mod.
        /// </summary>
        [HarmonyPatch(typeof(GhostUtility), nameof(GhostUtility.GhostGraphicFor))]
        public static class GhostArt
        {
            // The game caches ghosts by the graphic's path and colours, so looks at the same path share one. Each look's
            // replacement is cached by the look's graphic and the game's ghost, both by reference.
            private static readonly Dictionary<Graphic, Dictionary<Graphic, Graphic>> ghosts =
                new Dictionary<Graphic, Dictionary<Graphic, Graphic>>(ReferenceEqualityComparer.Instance);

            private static void Postfix(Graphic baseGraphic, ref Graphic __result)
            {
                if (!(baseGraphic?.data is GraphicData_FromMod fromModData) || __result?.data == null ||
                    __result.data is GraphicData_FromMod)
                {
                    return;
                }
                try
                {
                    if (!ghosts.TryGetValue(baseGraphic, out Dictionary<Graphic, Graphic> lookGhosts))
                    {
                        lookGhosts = new Dictionary<Graphic, Graphic>(ReferenceEqualityComparer.Instance);
                        ghosts[baseGraphic] = lookGhosts;
                    }
                    if (!lookGhosts.TryGetValue(__result, out Graphic ghost))
                    {
                        GraphicData_FromMod ghostData = fromModData.CopyArtSource();
                        ghostData.CopyFrom(__result.data);
                        ghost = GraphicDatabase.Get(__result.GetType(), __result.path, __result.Shader, __result.drawSize,
                            __result.color, __result.colorTwo, ghostData, null, __result.maskPath);
                        lookGhosts[__result] = ghost;
                    }
                    __result = ghost;
                }
                catch (Exception e)
                {
                    Log.WarningOnce($"[Visual Progression] Couldn't keep the placing preview of the look at {baseGraphic.path} on " +
                        $"art from {SourceOf(fromModData)}, so it shows the normal art: " + e, ("VP_FromModGhost_" + baseGraphic.path).GetHashCode());
                }
            }
        }

        /// <summary>
        /// Keeps a resized copy of a look's graphic, like packed furniture bigger than one tile, on the chosen mod's art. The
        /// game makes the copy without graphic data, so it's made again with data that names only where the art comes from,
        /// which is what the art hooks read; every other setting keeps the game's default, as for a copy without data.
        /// </summary>
        [HarmonyPatch(typeof(Graphic), nameof(Graphic.GetCopy), new[] { typeof(Vector2), typeof(Shader) })]
        public static class ResizedCopyArt
        {
            // The game caches graphics by their graphic data, so each look keeps one data for its copies.
            private static readonly Dictionary<GraphicData_FromMod, GraphicData_FromMod> copyData =
                new Dictionary<GraphicData_FromMod, GraphicData_FromMod>();

            private static void Postfix(Graphic __instance, Vector2 newDrawSize, Shader overrideShader, ref Graphic __result)
            {
                if (!(__instance.data is GraphicData_FromMod fromModData) || __result?.data != null)
                {
                    return;
                }
                try
                {
                    if (!copyData.TryGetValue(fromModData, out GraphicData_FromMod data))
                    {
                        data = fromModData.CopyArtSource();
                        data.texPath = fromModData.texPath;
                        copyData[fromModData] = data;
                    }
                    __result = GraphicDatabase.Get(__instance.GetType(), __instance.path, overrideShader ?? __instance.Shader,
                        newDrawSize, __instance.color, __instance.colorTwo, data, null, null);
                }
                catch (Exception e)
                {
                    Log.WarningOnce($"[Visual Progression] Couldn't keep a resized copy of the look at {__instance.path} on " +
                        $"art from {SourceOf(fromModData)}, so it shows the normal art: " + e, ("VP_FromModCopy_" + __instance.path).GetHashCode());
                }
            }
        }

        /// <summary>
        /// Keeps a look's blueprint on the chosen mod's art. The game makes the blueprint's graphic data as a plain copy of
        /// the look's, just before it resolves the look's icon.
        /// </summary>
        [HarmonyPatch(typeof(ThingStyleDef), "ResolveUIIcon")]
        public static class BlueprintArt
        {
            private static void Postfix(ThingStyleDef __instance)
            {
                if (!(__instance.graphicData is GraphicData_FromMod fromModData) || __instance.blueprintGraphicData == null ||
                    __instance.blueprintGraphicData is GraphicData_FromMod)
                {
                    return;
                }
                try
                {
                    GraphicData_FromMod blueprint = fromModData.CopyArtSource();
                    blueprint.CopyFrom(__instance.blueprintGraphicData);
                    __instance.blueprintGraphicData = blueprint;
                }
                catch (Exception e)
                {
                    Log.WarningOnce($"[Visual Progression] Couldn't keep the blueprint of {__instance.defName} on art from " +
                        $"{SourceOf(fromModData)}, so it shows the normal art: " + e, ("VP_FromModBlueprint_" + __instance.defName).GetHashCode());
                }
            }
        }

        /// <summary>
        /// The running mod a graphic's art at the path comes from: the named mod, or with skipped mods the last loaded other
        /// mod that has the art, as the game would pick it. Null (with a warning once) when there's none, and the game's
        /// own lookup is used.
        /// </summary>
        private static ModContentPack ChosenMod(GraphicData_FromMod fromModData, string path, Func<ModContentPack, bool> hasArt)
        {
            if (fromModData.SkipsMods)
            {
                return WinnerSkipping(fromModData, path, hasArt);
            }
            // The item's own mod is filled in when references are resolved, and some loaders build graphics before that.
            // The look is built again once it's filled in, and an item without a mod gets its own warning then.
            if (fromModData.fromMod.NullOrEmpty())
            {
                return null;
            }
            if (!fromModData.skipMods.NullOrEmpty())
            {
                Log.WarningOnce($"[Visual Progression] The look at {fromModData.texPath} names both a mod and mods to skip, " +
                    $"so its art comes from {fromModData.fromMod}.", ("VP_FromModAndSkip_" + fromModData.texPath).GetHashCode());
            }
            ModContentPack mod = RunningMod(fromModData.fromMod);
            if (mod == null)
            {
                // Stack and random graphics come here once per image, so this warns once per look.
                string look = fromModData.texPath;
                Log.WarningOnce($"[Visual Progression] The look at {look} wants art from \"{fromModData.fromMod}\", " +
                    "which isn't active, so it shows the normal art.", ("VP_FromModInactive_" + fromModData.fromMod + look).GetHashCode());
            }
            return mod;
        }

        // The game looks a path up in the running mods from the last loaded to the first, then in Core's built-in
        // resources, which ModOwnTextures checks with Core. So Core comes last.
        private static ModContentPack WinnerSkipping(GraphicData_FromMod fromModData, string path, Func<ModContentPack, bool> hasArt)
        {
            List<ModContentPack> mods = LoadedModManager.RunningModsListForReading;
            ModContentPack core = null;
            for (int i = mods.Count - 1; i >= 0; i--)
            {
                if (mods[i].IsCoreMod)
                {
                    core = mods[i];
                }
                else if (!Skipped(fromModData, mods[i]) && hasArt(mods[i]))
                {
                    return mods[i];
                }
            }
            if (core != null && !Skipped(fromModData, core) && hasArt(core))
            {
                return core;
            }
            Log.WarningOnce($"[Visual Progression] Only the mods it skips ({string.Join(", ", fromModData.skipMods)}) have art " +
                $"at {path}, so it shows the normal art.",
                ("VP_SkipModsNoArt_" + path).GetHashCode());
            return null;
        }

        private static bool Skipped(GraphicData_FromMod fromModData, ModContentPack mod)
        {
            foreach (string packageId in fromModData.skipMods)
            {
                if (string.Equals(mod.PackageIdPlayerFacing, packageId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        // Where a graphic's art comes from, for warnings.
        private static string SourceOf(GraphicData_FromMod fromModData)
        {
            return fromModData.SkipsMods
                ? "any mod but " + string.Join(", ", fromModData.skipMods)
                : fromModData.fromMod;
        }

        private static ModContentPack RunningMod(string packageId)
        {
            // PackageId gains "_steam" when a mod is installed both locally and from the Workshop. The player-facing one doesn't.
            foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
            {
                if (string.Equals(mod.PackageIdPlayerFacing, packageId, StringComparison.OrdinalIgnoreCase))
                {
                    return mod;
                }
            }
            return null;
        }
    }
}
