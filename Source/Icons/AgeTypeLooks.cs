using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Swaps each listed item type's icon and name for the look of the given age, and back past its last row. Bills, the
    /// research tab, info cards, storage filters and popups show the type, so they show what the item would look like and
    /// be called if made now.
    /// </summary>
    public static class AgeTypeLooks
    {
        private const int WarningKey = 0x56504733;

        private static AccessTools.FieldRef<CompProperties_Refuelable, Texture2D> cachedFuelIcon;
        private static List<TypeLook> types;
        private static Dictionary<ThingDef, TypeLook> typesByDef;
        private static TechLevel appliedAge = TechLevel.Undefined;

        /// <summary>Gives every listed item type the icon and name for this age. Does nothing when the age hasn't changed.</summary>
        public static void ApplyFor(TechLevel age)
        {
            if (age == appliedAge)
            {
                return;
            }
            if (types == null)
            {
                types = ListedTypes();
                typesByDef = types.ToDictionary(type => type.Def);
            }
            foreach (TypeLook type in types)
            {
                type.ShowFor(age);
            }
            appliedAge = age;
            // Item labels are cached with the type's name in them.
            GenLabel.ClearCache();
            RefreshCachedIcons();
        }

        /// <summary>The type's name from load while its name follows the age, or null when it shows that name anyway.</summary>
        internal static string NormalNameWhileSwapped(ThingDef def)
        {
            if (typesByDef == null || !typesByDef.TryGetValue(def, out TypeLook type))
            {
                return null;
            }
            return type.NameIsSwapped ? type.NormalName : null;
        }

        // These read a type's icon once and keep it, so they're read again after each swap.
        private static void RefreshCachedIcons()
        {
            try
            {
                cachedFuelIcon ??= AccessTools.FieldRefAccess<CompProperties_Refuelable, Texture2D>("fuelIcon");
                HashSet<ThingDef> listed = types.Where(type => type.SwapsIcon).Select(type => type.Def).ToHashSet();
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    CompProperties_Refuelable refuelable = def.GetCompProperties<CompProperties_Refuelable>();
                    // The game's own fallback when the filter allows nothing.
                    ThingDef fuel = refuelable?.fuelFilter?.AnyAllowedDef ?? ThingDefOf.Chemfuel;
                    if (refuelable != null && refuelable.fuelIconPath.NullOrEmpty() && listed.Contains(fuel))
                    {
                        cachedFuelIcon(refuelable) = null;
                    }
                }
                if (types.Any(type => type.SwapsIcon && type.Def.IsMedicine))
                {
                    MedicalCareUtility.Reset();
                }
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Visual Progression] Couldn't refresh the fuel and medical care icons, so they may show an older age: " + e, WarningKey);
            }
        }

        private static List<TypeLook> ListedTypes()
        {
            return DefDatabase<ThingDef>.AllDefsListForReading
                .Select(def => (def, props: def.GetCompProperties<CompProperties_AgeLook>()))
                .Where(entry => entry.props != null && !entry.props.looks.NullOrEmpty())
                .Select(entry => new TypeLook(entry.def, entry.props.looks))
                .Where(type => type.SwapsIcon || type.SwapsName)
                .ToList();
        }

        /// <summary>One item type, with the icon and name it had at load.</summary>
        private sealed class TypeLook
        {
            private readonly ThingDef def;
            private readonly List<AgeLookRow> looks;
            private readonly List<int> upToAges;
            private readonly List<string> rowNames;
            private readonly Texture2D normalIcon;
            private readonly Material normalMaterial;
            private readonly string normalName;

            public TypeLook(ThingDef def, List<AgeLookRow> looks)
            {
                this.def = def;
                this.looks = looks;
                upToAges = looks.Select(row => (int)row.upToAge).ToList();
                rowNames = looks.Select(row => row.style?.overrideLabel).ToList();
                normalIcon = def.uiIcon;
                normalMaterial = def.uiIconMaterial;
                normalName = def.label;
            }

            public ThingDef Def => def;

            public string NormalName => normalName;

            public bool NameIsSwapped => def.label != normalName;

            // Items with their own icon file are left out: their unstyled copies draw the type's icon too.
            public bool SwapsIcon => def.uiIconPath.NullOrEmpty() && def.uiIconPathsStuff == null;

            // A material without an adjective lends its name to everything made from it.
            public bool SwapsName => !(def.IsStuff && def.stuffProps.stuffAdjective.NullOrEmpty());

            public void ShowFor(TechLevel age)
            {
                if (SwapsIcon)
                {
                    ShowIconFor(age);
                }
                if (SwapsName)
                {
                    string name = AgeNames.TypeNameFor(upToAges, rowNames, normalName, (int)age);
                    if (def.label != name)
                    {
                        def.label = name;
                        def.ClearCachedData();
                    }
                }
            }

            private void ShowIconFor(TechLevel age)
            {
                int rowIndex = AgeLookTable.RowFor(upToAges, (int)age);
                ThingStyleDef style = rowIndex >= 0 ? looks[rowIndex].style : null;
                if (style?.UIIcon != null)
                {
                    // Drawn as the game draws a style's icon, in the type's icon color, unless the look draws through a
                    // mask. A material would drop the color of the default material for items made from one, and an
                    // icon file of the look's own isn't the picture the mask is shaped for.
                    def.uiIcon = style.UIIcon;
                    def.uiIconMaterial = def.MadeFromStuff || style.Graphic == null || !style.uiIconPath.NullOrEmpty()
                        ? null
                        : LookIconMaterials.IconMaterialFor(style.Graphic.ExtractInnerGraphicFor(null).MatAt(Rot4.North));
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
