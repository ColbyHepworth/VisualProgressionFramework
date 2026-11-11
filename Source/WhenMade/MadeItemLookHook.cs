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
    /// step, so an item gets its look as it's made, before it can join a stack on the ground. A harvested organ gets the
    /// colony's age too. An implant removed from a body gets the age of the patient's faction, since it came from them.
    /// </summary>
    public static class MadeItemLookHook
    {
        private const int WarningKey = 0x56504735;
        private static bool makingProducts;
        private static Faction surgeryPatientFaction;
        private static HashSet<ThingDef> implants;

        [HarmonyPatch(typeof(Bill_Medical), nameof(Bill_Medical.Notify_IterationCompleted))]
        public static class DuringSurgery
        {
            // Read before the operation runs, since it can kill the patient.
            private static void Prefix(Bill_Medical __instance, out (bool makingProducts, Faction patientFaction) __state)
            {
                __state = (makingProducts, surgeryPatientFaction);
                makingProducts = true;
                surgeryPatientFaction = __instance.GiverPawn?.Faction;
            }

            private static void Finalizer((bool makingProducts, Faction patientFaction) __state)
            {
                makingProducts = __state.makingProducts;
                surgeryPatientFaction = __state.patientFaction;
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
                    CompAgeLook look = __result.TryGetComp<CompAgeLook>();
                    if (look == null)
                    {
                        return;
                    }
                    // Outside a surgery there's no patient, so an implant a job makes keeps its normal art.
                    if (Implants().Contains(__result.def))
                    {
                        look.ApplyLookForFactionAge(surgeryPatientFaction);
                    }
                    else
                    {
                        look.ApplyLookForColonyAge();
                    }
                }
                catch (Exception e)
                {
                    Log.WarningOnce("[Visual Progression] Couldn't give a made item its look: " + e, WarningKey);
                }
            }
        }

        // Items that come out of a body when an implant is removed, like bionics, glands or a peg leg's wood. Several game
        // and mod surgeries spawn them, each its own way, so they're recognized by what they are rather than by which
        // surgery made them. Natural organs come from body parts instead, and get the colony's age like other products.
        private static HashSet<ThingDef> Implants()
        {
            if (implants == null)
            {
                implants = new HashSet<ThingDef>();
                foreach (HediffDef hediff in DefDatabase<HediffDef>.AllDefsListForReading)
                {
                    if (hediff.spawnThingOnRemoved != null)
                    {
                        implants.Add(hediff.spawnThingOnRemoved);
                    }
                }
            }
            return implants;
        }
    }
}
