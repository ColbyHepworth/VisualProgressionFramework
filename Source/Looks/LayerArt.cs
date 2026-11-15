using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Gives the layers a building's own code draws over its art (a toilet's raised lid, a bathtub's water) the art source
    /// of the building's look, or of its normal art when that names a source. A layer is an image whose path starts with
    /// the building's own art path. The building's draw and print methods are found at startup and mark the building while
    /// they run, and while one is marked its layers' materials are swapped for the same images from that source.
    /// </summary>
    public static class LayerArt
    {
        private static readonly HashSet<ThingDef> buildingsWithArtSources = new HashSet<ThingDef>();
        private static readonly HashSet<(Module, int)> hookedMethods = new HashSet<(Module, int)>();

        // Each layer graphic's replacement for each art source, both by reference. Null when the source has no such image.
        private static readonly Dictionary<Graphic, Dictionary<GraphicData, Graphic>> replacements =
            new Dictionary<Graphic, Dictionary<GraphicData, Graphic>>(ReferenceEqualityComparer.Instance);

        [ThreadStatic] private static Thing drawingBuilding;
        [ThreadStatic] private static GraphicData drawingSource;
        [ThreadStatic] private static bool swapping;

        /// <summary>
        /// Hooks the draw and print methods of every building with looks or with normal art that names a source. Buildings
        /// share some of these methods, so each is hooked once.
        /// </summary>
        internal static void HookBuildingsWithArtSources(Harmony harmony)
        {
            Stopwatch timer = Stopwatch.StartNew();
            var thingClasses = new HashSet<Type>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.category != ThingCategory.Building || !HasArtSource(def))
                {
                    continue;
                }
                buildingsWithArtSources.Add(def);
                thingClasses.Add(def.thingClass);
            }
            foreach (Type thingClass in thingClasses)
            {
                for (Type type = thingClass; type != null && typeof(ThingWithComps).IsAssignableFrom(type); type = type.BaseType)
                {
                    foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        if ((method.Name == "DrawAt" || method.Name == "Print") && !method.IsAbstract)
                        {
                            Hook(harmony, method);
                        }
                    }
                }
            }
            if (Prefs.DevMode)
            {
                Log.Message($"[Visual Progression] Watching {hookedMethods.Count} draw methods of {buildingsWithArtSources.Count} "
                    + $"buildings for layers, found in {timer.ElapsedMilliseconds} ms.");
            }
        }

        private static bool HasArtSource(ThingDef def)
        {
            if (def.graphicData is GraphicData_FromMod)
            {
                return true;
            }
            CompProperties_AgeLook looks = def.GetCompProperties<CompProperties_AgeLook>();
            return looks != null && !looks.looks.NullOrEmpty();
        }

        private static void Hook(Harmony harmony, MethodInfo method)
        {
            if (!hookedMethods.Add((method.Module, method.MetadataToken)))
            {
                return;
            }
            try
            {
                harmony.Patch(method,
                    prefix: new HarmonyMethod(typeof(LayerArt), nameof(MarkPrefix)),
                    finalizer: new HarmonyMethod(typeof(LayerArt), nameof(MarkFinalizer)));
            }
            catch (Exception e)
            {
                string name = $"{method.DeclaringType?.FullName}.{method.Name}";
                Log.WarningOnce($"[Visual Progression] Couldn't watch {name}, so layers it draws show the art of whichever mod "
                    + "wins: " + e, ("VP_LayerHook_" + name).GetHashCode());
            }
        }

        private static void MarkPrefix(Thing __instance, out (Thing, GraphicData) __state)
        {
            __state = (drawingBuilding, drawingSource);
            GraphicData source = buildingsWithArtSources.Contains(__instance.def) ? ArtSourceOf(__instance) : null;
            drawingBuilding = source == null ? null : __instance;
            drawingSource = source;
        }

        private static void MarkFinalizer((Thing, GraphicData) __state)
        {
            (drawingBuilding, drawingSource) = __state;
        }

        // An ideology style isn't a look, so it leaves the building's layers alone.
        private static GraphicData ArtSourceOf(Thing building)
        {
            ThingStyleDef style = building.StyleDef;
            if (style != null)
            {
                return style.graphicData != null && building.TryGetComp<CompAgeLook>()?.HasAgeLook == true ? style.graphicData : null;
            }
            return building.def.graphicData as GraphicData_FromMod;
        }

        /// <summary>
        /// Swaps a layer's material while its building draws. Four-facing art reads its materials in the base method,
        /// which this hook covers.
        /// </summary>
        [HarmonyPatch(typeof(Graphic), nameof(Graphic.MatAt))]
        public static class LayerMaterial
        {
            private static void Postfix(Graphic __instance, Rot4 rot, Thing thing, ref Material __result)
            {
                SwapForLayer(__instance, rot, thing, ref __result);
            }
        }

        /// <summary>Swaps a single picture's layer material while its building draws.</summary>
        [HarmonyPatch(typeof(Graphic_Single), nameof(Graphic_Single.MatAt))]
        public static class SingleLayerMaterial
        {
            private static void Postfix(Graphic_Single __instance, Rot4 rot, Thing thing, ref Material __result)
            {
                SwapForLayer(__instance, rot, thing, ref __result);
            }
        }

        private static void SwapForLayer(Graphic graphic, Rot4 rot, Thing thing, ref Material material)
        {
            Thing building = drawingBuilding;
            if (building == null || swapping || !UnityData.IsInMainThread)
            {
                return;
            }
            Graphic replacement = ReplacementFor(graphic, building, drawingSource);
            if (replacement == null)
            {
                return;
            }
            swapping = true;
            try
            {
                material = replacement.MatAt(rot, thing);
            }
            finally
            {
                swapping = false;
            }
        }

        private static Graphic ReplacementFor(Graphic graphic, Thing building, GraphicData source)
        {
            // Pinned art already comes from its source, and the building's own art is the look's business.
            if (graphic.data is GraphicData_FromMod || graphic.data == source || graphic.data == building.def.graphicData)
            {
                return null;
            }
            if (!replacements.TryGetValue(graphic, out Dictionary<GraphicData, Graphic> bySource))
            {
                bySource = new Dictionary<GraphicData, Graphic>(ReferenceEqualityComparer.Instance);
                replacements[graphic] = bySource;
            }
            if (!bySource.TryGetValue(source, out Graphic replacement))
            {
                try
                {
                    string layerPath = LayerPath(building.def, graphic.path, source);
                    replacement = layerPath == null ? null : Build(graphic, layerPath, source);
                }
                catch (Exception e)
                {
                    replacement = null;
                    Log.WarningOnce($"[Visual Progression] Couldn't give the layer at {graphic.path} of {building.def.defName} its " +
                        "building's art, so it shows the art of whichever mod wins: " + e, ("VP_Layer_" + graphic.path).GetHashCode());
                }
                bySource[source] = replacement;
            }
            return replacement;
        }

        // A layer is named after the building's own art, like "bath" and "bathWater", and moves with the source's path.
        // Images in a folder at the building's path ("shrine/Granite") are its own art's, not layers.
        private static string LayerPath(ThingDef def, string path, GraphicData source)
        {
            string ownPath = def.graphicData?.texPath;
            if (path.NullOrEmpty() || ownPath.NullOrEmpty() || path.Length <= ownPath.Length || !path.StartsWith(ownPath, StringComparison.Ordinal)
                || path[ownPath.Length] == '/')
            {
                return null;
            }
            return source.texPath + path.Substring(ownPath.Length);
        }

        private static Graphic Build(Graphic layer, string layerPath, GraphicData source)
        {
            bool facings = layer is Graphic_Multi;
            if (source is GraphicData_FromMod fromModData)
            {
                ModContentPack mod = ArtFromModHooks.ChosenMod(fromModData, layerPath, candidate => HasArt(candidate, layerPath, facings), quiet: true);
                if (mod == null || !HasArt(mod, layerPath, facings))
                {
                    return null;
                }
                GraphicData_FromMod data = fromModData.CopyArtSource();
                if (layer.data != null)
                {
                    data.CopyFrom(layer.data);
                }
                data.texPath = layerPath;
                data.graphicClass = layer.GetType();
                return GraphicDatabase.Get(layer.GetType(), layerPath, layer.Shader, layer.drawSize, layer.color, layer.colorTwo, data,
                    layer.data?.shaderParameters, layerPath == layer.path ? layer.maskPath : null);
            }
            // A look with plain art changes only layers named after its path, from whichever mod wins.
            if (layerPath == layer.path || !ExistsAnywhere(layerPath, facings))
            {
                return null;
            }
            return GraphicDatabase.Get(layer.GetType(), layerPath, layer.Shader, layer.drawSize, layer.color, layer.colorTwo, null,
                layer.data?.shaderParameters, null);
        }

        private static bool HasArt(ModContentPack mod, string path, bool facings)
        {
            return facings ? ArtFromModHooks.HasFacingArt(mod, path) : ModOwnTextures.From(mod, path, out _) != null;
        }

        private static bool ExistsAnywhere(string path, bool facings)
        {
            if (ContentFinder<Texture2D>.Get(path, reportFailure: false) != null)
            {
                return true;
            }
            return facings && (ContentFinder<Texture2D>.Get(path + Graphic_Multi.NorthSuffix, reportFailure: false) != null
                || ContentFinder<Texture2D>.Get(path + Graphic_Multi.SouthSuffix, reportFailure: false) != null
                || ContentFinder<Texture2D>.Get(path + Graphic_Multi.EastSuffix, reportFailure: false) != null
                || ContentFinder<Texture2D>.Get(path + Graphic_Multi.WestSuffix, reportFailure: false) != null);
        }
    }

    /// <summary>Hooks buildings' draw methods for layers once defs, looks included, are loaded.</summary>
    [StaticConstructorOnStartup]
    public static class LayerArtAtStartup
    {
        static LayerArtAtStartup()
        {
            try
            {
                LayerArt.HookBuildingsWithArtSources(new Harmony("groovy.visualprogression.framework"));
            }
            catch (Exception e)
            {
                Log.Warning("[Visual Progression] Couldn't find buildings' draw methods, so their layers show the art of whichever " +
                    "mod wins: " + e);
            }
        }
    }
}
