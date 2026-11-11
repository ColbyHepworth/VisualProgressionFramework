using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Gives items a surgery produces, like hemogen packs and ova, items gathered from an animal, like milk, wool and a
    /// boomalope's chemfuel, and items a colonist's job makes by name (found by <see cref="JobProductLookHook"/>) the look
    /// of the colony's age. They all make and place their items themselves instead of going through a bill's crafting
    /// step, so an item gets its look as it's made, before it can join a stack on the ground. Body parts and implants
    /// removed from a body weren't made then, so they keep their normal art.
    /// </summary>
    public static class MadeItemLookHook
    {
        private const int WarningKey = 0x56504735;
        private static bool makingProducts;
        private static HashSet<ThingDef> removableParts;

        [HarmonyPatch(typeof(Bill_Medical), nameof(Bill_Medical.Notify_IterationCompleted))]
        public static class DuringSurgery
        {
            private static void Prefix(out bool __state)
            {
                __state = makingProducts;
                makingProducts = true;
            }

            private static void Finalizer(bool __state)
            {
                makingProducts = __state;
            }
        }

        /// <summary>The game's milking and shearing, and mods' that build on it, gather through this one method.</summary>
        [HarmonyPatch(typeof(CompHasGatherableBodyResource), nameof(CompHasGatherableBodyResource.Gathered))]
        public static class DuringGathering
        {
            private static void Prefix(out bool __state)
            {
                __state = makingProducts;
                makingProducts = true;
            }

            private static void Finalizer(bool __state)
            {
                makingProducts = __state;
            }
        }

        /// <summary>
        /// Animals from Vanilla Expanded mods (and others using its animal products) are gathered by its own job, which
        /// skips <see cref="CompHasGatherableBodyResource.Gathered"/>.
        /// </summary>
        [HarmonyPatch]
        public static class DuringExpandedGathering
        {
            private static bool Prepare()
            {
                return ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core") && TargetMethod() != null;
            }

            private static MethodBase TargetMethod()
            {
                return AccessTools.Method("VEF.AnimalBehaviours.CompAnimalProduct:InformGathered");
            }

            private static void Prefix(out bool __state)
            {
                __state = makingProducts;
                makingProducts = true;
            }

            private static void Finalizer(bool __state)
            {
                makingProducts = __state;
            }
        }

        [HarmonyPatch(typeof(ThingMaker), nameof(ThingMaker.MakeThing))]
        public static class MadeItem
        {
            private static void Postfix(Thing __result)
            {
                if (__result == null || (!makingProducts && !JobProductLookHook.IsMaking(__result.def)))
                {
                    return;
                }
                // An error here would stop the surgery, gathering or job partway, after its other effects.
                try
                {
                    if (!RemovableParts().Contains(__result.def))
                    {
                        __result.TryGetComp<CompAgeLook>()?.ApplyLookForColonyAge();
                    }
                }
                catch (Exception e)
                {
                    Log.WarningOnce("[Visual Progression] Couldn't give a made item its look: " + e, WarningKey);
                }
            }
        }

        // Items that come out of a body when a part or implant is removed. Several game and mod surgeries spawn them, each
        // its own way, so they're recognized by what they are rather than by which surgery made them.
        private static HashSet<ThingDef> RemovableParts()
        {
            if (removableParts == null)
            {
                removableParts = new HashSet<ThingDef>();
                foreach (HediffDef hediff in DefDatabase<HediffDef>.AllDefsListForReading)
                {
                    if (hediff.spawnThingOnRemoved != null)
                    {
                        removableParts.Add(hediff.spawnThingOnRemoved);
                    }
                }
                foreach (BodyPartDef part in DefDatabase<BodyPartDef>.AllDefsListForReading)
                {
                    if (part.spawnThingOnRemoved != null)
                    {
                        removableParts.Add(part.spawnThingOnRemoved);
                    }
                }
            }
            return removableParts;
        }
    }
}
