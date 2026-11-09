using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using RimWorld.IO;
using UnityEngine;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Finds one mod's own image, even where another mod replaces it. Some loaders free a mod's copy of an image another
    /// mod replaces and point it at the replacement, so the copy the game holds for that mod is no longer its own. Then
    /// the image is read from the mod's files.
    /// </summary>
    public static class ModOwnTextures
    {
        private static readonly Type filesystemFileType = AccessTools.TypeByName("RimWorld.IO.FilesystemFile");
        private static readonly Dictionary<ModContentPack, Dictionary<string, FileInfo>> filesByMod = new Dictionary<ModContentPack, Dictionary<string, FileInfo>>();
        private static readonly Dictionary<(ModContentPack, string), Texture2D> loaded = new Dictionary<(ModContentPack, string), Texture2D>();

        /// <summary>
        /// The mod's own image at the path (under Textures, without an extension), or null with the reason in
        /// <paramref name="problem"/>. Call it on the main thread, like any texture lookup.
        /// </summary>
        public static Texture2D From(ModContentPack mod, string path, out string problem)
        {
            problem = null;
            Texture2D texture = mod.GetContentHolder<Texture2D>().Get(path);
            // The base game keeps its own art in its built-in resources rather than in its mod files.
            if (texture == null && mod.IsCoreMod)
            {
                texture = Resources.Load<Texture2D>(GenFilePaths.ContentPath<Texture2D>() + path);
            }
            else if (texture != null && HeldByLaterMod(mod, path, texture))
            {
                try
                {
                    texture = Load(mod, path);
                    if (texture == null)
                    {
                        problem = "a loader swapped its image for another mod's, and no file at that path was found in its own files";
                    }
                }
                catch (Exception e)
                {
                    texture = null;
                    problem = "a loader swapped its image for another mod's, and its own file couldn't be read: " + e;
                }
            }
            if (texture == null && problem == null)
            {
                problem = "it has no texture there";
            }
            return texture;
        }

        // Some loaders point a mod's copy of a replaced image at the replacing mod's, which loads later.
        private static bool HeldByLaterMod(ModContentPack mod, string path, Texture2D texture)
        {
            List<ModContentPack> mods = LoadedModManager.RunningModsListForReading;
            for (int i = mods.IndexOf(mod) + 1; i < mods.Count; i++)
            {
                if (ReferenceEquals(mods[i].GetContentHolder<Texture2D>().Get(path), texture))
                {
                    return true;
                }
            }
            return false;
        }

        private static Texture2D Load(ModContentPack mod, string path)
        {
            if (loaded.TryGetValue((mod, path), out Texture2D texture))
            {
                return texture;
            }
            texture = null;
            if (FilesOf(mod).TryGetValue(path, out FileInfo file))
            {
                texture = ModContentLoader<Texture2D>.LoadItem(GameFile(file))?.contentItem;
                if (texture == BaseContent.BadTex)
                {
                    texture = null;
                }
            }
            loaded[(mod, path)] = texture;
            return texture;
        }

        // The game's own file type reads packed DLC files too, but it isn't public.
        private static VirtualFile GameFile(FileInfo file)
        {
            if (filesystemFileType == null)
            {
                throw new InvalidOperationException("the game's RimWorld.IO.FilesystemFile type is gone");
            }
            return (VirtualFile)Activator.CreateInstance(filesystemFileType, file);
        }

        private static Dictionary<string, FileInfo> FilesOf(ModContentPack mod)
        {
            if (filesByMod.TryGetValue(mod, out Dictionary<string, FileInfo> files))
            {
                return files;
            }
            files = new Dictionary<string, FileInfo>();
            string texturesFolder = GenFilePaths.ContentPath<Texture2D>();
            foreach (KeyValuePair<string, FileInfo> entry in ModContentPack.GetAllFilesForMod(mod, texturesFolder, ModContentLoader<Texture2D>.IsAcceptableExtension))
            {
                string path = entry.Key.Replace('\\', '/');
                if (path.StartsWith(texturesFolder, StringComparison.OrdinalIgnoreCase))
                {
                    path = path.Substring(texturesFolder.Length);
                }
                // Keyed as the game keys its loaded textures: one extension off.
                path = path.Substring(0, path.Length - Path.GetExtension(path).Length);
                // The game loads a .dds in place of a same-named image in another format.
                bool isDds = entry.Value.Name.EndsWith(".dds", StringComparison.OrdinalIgnoreCase);
                if (!files.ContainsKey(path) || isDds)
                {
                    files[path] = entry.Value;
                }
            }
            filesByMod[mod] = files;
            return files;
        }
    }
}
