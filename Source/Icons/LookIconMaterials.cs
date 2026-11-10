using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Shows a look's mask in its icons. The game draws a style's icon as a plain picture, so a look that draws a symbol or
    /// colour regions through a mask lost them in every icon, though an item without a look shows its mask there.
    /// </summary>
    public static class LookIconMaterials
    {
        /// <summary>The icon version of the material, as the game makes for things without a style, or null without one.</summary>
        public static Material IconMaterialFor(Material material)
        {
            if (material == null || !ShaderDatabase.TryGetUIShader(material.shader, out Shader iconShader) ||
                !MaterialPool.TryGetRequestForMat(material, out MaterialRequest request))
            {
                return null;
            }
            request.shader = iconShader;
            return MaterialPool.MatFrom(request);
        }

        /// <summary>Gives a real item's icon its look's mask, the way the game draws an item without a style.</summary>
        [HarmonyPatch(typeof(Widgets), nameof(Widgets.GetIconFor), new[]
        {
            typeof(Thing), typeof(Vector2), typeof(Rot4?), typeof(bool), typeof(float), typeof(float), typeof(Vector2),
            typeof(Color), typeof(Material),
        }, new[]
        {
            ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out,
            ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out,
        })]
        public static class ItemIcon
        {
            // Runs for every item icon drawn each frame, so the cheapest checks come first.
            private static void Postfix(Thing thing, Rot4? rot, Texture __result, ref Color color, ref Material material)
            {
                if (material != null || __result == null || thing == null)
                {
                    return;
                }
                Thing item = thing.GetInnerIfMinified();
                if (item.StyleDef == null || item.UIIconOverride != null || !(item.TryGetComp<CompAgeLook>()?.HasAgeLook ?? false))
                {
                    return;
                }
                Material drawn = item.Graphic.ExtractInnerGraphicFor(item).MatAt(rot ?? item.def.defaultPlacingRot);
                // Only when the icon is that picture, so a stack shown as one item keeps the game's icon.
                if (drawn == null || drawn.mainTexture != __result)
                {
                    return;
                }
                Material iconMaterial = IconMaterialFor(drawn);
                if (iconMaterial != null)
                {
                    material = iconMaterial;
                    color = Color.white;
                }
            }
        }
    }
}
