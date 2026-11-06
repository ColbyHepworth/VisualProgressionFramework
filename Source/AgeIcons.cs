using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Swaps each listed item type's icon for the look of the given age, and back past its last row. Bills, the research
    /// tab and info cards draw the type's icon, so they show what the item would look like if made now.
    /// </summary>
    public static class AgeIcons
    {
        private static List<TypeIcon> types;
        private static TechLevel appliedAge = TechLevel.Undefined;

        /// <summary>Gives every listed item type the icon for this age. Does nothing when the age hasn't changed.</summary>
        public static void ApplyFor(TechLevel age)
        {
            if (age == appliedAge)
            {
                return;
            }
            types ??= ListedTypes();
            foreach (TypeIcon type in types)
            {
                type.ShowFor(age);
            }
            appliedAge = age;
        }

        // Items with their own icon file are left out: their unstyled copies draw the type's icon too.
        private static List<TypeIcon> ListedTypes()
        {
            return DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.uiIconPath.NullOrEmpty() && def.uiIconPathsStuff == null)
                .Select(def => (def, props: def.GetCompProperties<CompProperties_AgeLook>()))
                .Where(entry => entry.props != null && !entry.props.looks.NullOrEmpty())
                .Select(entry => new TypeIcon(entry.def, entry.props.looks))
                .ToList();
        }

        /// <summary>One item type, with the icon it had at load.</summary>
        private sealed class TypeIcon
        {
            private readonly ThingDef def;
            private readonly List<AgeLookRow> looks;
            private readonly List<int> upToAges;
            private readonly Texture2D normalIcon;
            private readonly Material normalMaterial;

            public TypeIcon(ThingDef def, List<AgeLookRow> looks)
            {
                this.def = def;
                this.looks = looks;
                upToAges = looks.Select(row => (int)row.upToAge).ToList();
                normalIcon = def.uiIcon;
                normalMaterial = def.uiIconMaterial;
            }

            public void ShowFor(TechLevel age)
            {
                int rowIndex = AgeLookTable.RowFor(upToAges, (int)age);
                Texture2D styleIcon = rowIndex >= 0 ? looks[rowIndex].style?.UIIcon : null;
                if (styleIcon != null)
                {
                    // Drawn as the game draws a style's icon: no material, the type's icon color.
                    def.uiIcon = styleIcon;
                    def.uiIconMaterial = null;
                }
                else
                {
                    def.uiIcon = normalIcon;
                    def.uiIconMaterial = normalMaterial;
                }
            }
        }
    }
}
