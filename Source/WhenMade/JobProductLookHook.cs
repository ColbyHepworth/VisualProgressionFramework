using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace VisualProgression
{
    /// <summary>
    /// Finds where jobs make a listed item by name, like Vanilla Races Expanded - Sanguophage's hemogen extraction, and
    /// marks those spots while they run so <see cref="MadeItemLookHook"/> gives the item the colony's age. Jobs are read
    /// once at startup and only the spots found are hooked, so nothing runs while pawns work.
    /// </summary>
    public static class JobProductLookHook
    {
        private const int WarningKey = 0x56504A42;
        private static readonly Dictionary<(Module, int), HashSet<ThingDef>> itemsByMethod = new Dictionary<(Module, int), HashSet<ThingDef>>();
        private static HashSet<ThingDef> itemsBeingMade;

        /// <summary>Whether a marked job is running and names this item.</summary>
        public static bool IsMaking(ThingDef def)
        {
            return itemsBeingMade != null && itemsBeingMade.Contains(def);
        }

        /// <summary>Reads every job driver and hooks the methods that make a listed item by name.</summary>
        public static void HookJobsThatMakeListedItems(Harmony harmony)
        {
            Stopwatch timer = Stopwatch.StartNew();
            Dictionary<(Module, int), ThingDef> itemFields = FieldsHoldingListedItems();
            if (itemFields.Count == 0)
            {
                return;
            }
            HashSet<(Module, int)> makeCalls = MakeCalls();
            int unreadable = 0;
            foreach (MethodInfo method in JobMethods(() => unreadable++))
            {
                IReadOnlyCollection<string> made;
                try
                {
                    made = MadeByName.ItemsMadeByName(StepsOf(method, itemFields, makeCalls));
                }
                catch (Exception)
                {
                    // Usually code that refers to a mod that isn't running.
                    unreadable++;
                    continue;
                }
                if (made.Count > 0)
                {
                    Hook(harmony, method, made.Select(defName => DefDatabase<ThingDef>.GetNamed(defName)).ToHashSet());
                }
            }
            if (Prefs.DevMode)
            {
                Log.Message($"[Visual Progression] Watching {itemsByMethod.Count} parts of jobs that make listed items, found in "
                    + $"{timer.ElapsedMilliseconds} ms ({unreadable} parts couldn't be read).");
            }
        }

        // DefOf fields are how code names a def. The game fills them before startup classes run.
        private static Dictionary<(Module, int), ThingDef> FieldsHoldingListedItems()
        {
            HashSet<ThingDef> listed = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.GetCompProperties<CompProperties_AgeLook>() != null)
                .ToHashSet();
            var fields = new Dictionary<(Module, int), ThingDef>();
            if (listed.Count == 0)
            {
                return fields;
            }
            foreach (Type type in GenTypes.AllTypesWithAttribute<DefOf>())
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (field.FieldType == typeof(ThingDef) && field.GetValue(null) is ThingDef def && listed.Contains(def))
                    {
                        fields[(field.Module, field.MetadataToken)] = def;
                    }
                }
            }
            return fields;
        }

        private static HashSet<(Module, int)> MakeCalls()
        {
            var makers = new List<MethodBase>
            {
                AccessTools.Method(typeof(ThingMaker), nameof(ThingMaker.MakeThing)),
                AccessTools.Method(typeof(GenSpawn), nameof(GenSpawn.Spawn), new[] { typeof(ThingDef), typeof(IntVec3), typeof(Map), typeof(WipeMode) }),
                AccessTools.Method(typeof(GenSpawn), nameof(GenSpawn.Spawn), new[] { typeof(ThingDef), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode) }),
            };
            return makers.Where(maker => maker != null).Select(maker => (maker.Module, maker.MetadataToken)).ToHashSet();
        }

        // A job's toils run as lambdas and iterators, which the compiler puts in types nested in the job driver.
        private static IEnumerable<MethodInfo> JobMethods(Action countUnreadable)
        {
            const BindingFlags declared = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static
                | BindingFlags.Public | BindingFlags.NonPublic;
            var types = new Stack<Type>(GenTypes.AllSubclasses(typeof(JobDriver)));
            var seen = new HashSet<Type>();
            while (types.Count > 0)
            {
                Type type = types.Pop();
                if (!seen.Add(type) || type.ContainsGenericParameters)
                {
                    continue;
                }
                Type[] nestedTypes;
                MethodInfo[] methods;
                try
                {
                    nestedTypes = type.GetNestedTypes(declared);
                    methods = type.GetMethods(declared);
                }
                catch (Exception)
                {
                    countUnreadable();
                    continue;
                }
                foreach (Type nested in nestedTypes)
                {
                    types.Push(nested);
                }
                foreach (MethodInfo method in methods)
                {
                    if (!method.IsAbstract && !method.ContainsGenericParameters)
                    {
                        yield return method;
                    }
                }
            }
        }

        private static IEnumerable<CodeStep> StepsOf(MethodInfo method, Dictionary<(Module, int), ThingDef> itemFields, HashSet<(Module, int)> makeCalls)
        {
            var steps = new List<CodeStep>();
            foreach (KeyValuePair<OpCode, object> instruction in PatchProcessor.ReadMethodBody(method))
            {
                if (instruction.Key == OpCodes.Ldsfld && instruction.Value is FieldInfo field
                    && itemFields.TryGetValue((field.Module, field.MetadataToken), out ThingDef item))
                {
                    steps.Add(CodeStep.LoadsItem(item.defName));
                }
                else if ((instruction.Key == OpCodes.Call || instruction.Key == OpCodes.Callvirt) && instruction.Value is MethodBase called
                    && makeCalls.Contains((called.Module, called.MetadataToken)))
                {
                    steps.Add(CodeStep.MakesThing);
                }
                else
                {
                    steps.Add(CodeStep.Other);
                }
            }
            return steps;
        }

        private static void Hook(Harmony harmony, MethodInfo method, HashSet<ThingDef> items)
        {
            (Module, int) key = (method.Module, method.MetadataToken);
            try
            {
                itemsByMethod[key] = items;
                harmony.Patch(method,
                    prefix: new HarmonyMethod(typeof(JobProductLookHook), nameof(MarkPrefix)),
                    finalizer: new HarmonyMethod(typeof(JobProductLookHook), nameof(MarkFinalizer)));
            }
            catch (Exception e)
            {
                itemsByMethod.Remove(key);
                Log.WarningOnce($"[Visual Progression] Couldn't watch {method.DeclaringType?.FullName}.{method.Name}, so items it "
                    + "makes keep their normal art: " + e, WarningKey);
            }
        }

        private static void MarkPrefix(MethodBase __originalMethod, out HashSet<ThingDef> __state)
        {
            __state = itemsBeingMade;
            itemsByMethod.TryGetValue((__originalMethod.Module, __originalMethod.MetadataToken), out itemsBeingMade);
        }

        private static void MarkFinalizer(HashSet<ThingDef> __state)
        {
            itemsBeingMade = __state;
        }
    }

    /// <summary>Runs the job scan once defs and their DefOf fields are loaded.</summary>
    [StaticConstructorOnStartup]
    public static class JobScanAtStartup
    {
        static JobScanAtStartup()
        {
            try
            {
                JobProductLookHook.HookJobsThatMakeListedItems(new Harmony("groovy.visualprogression.framework"));
            }
            catch (Exception e)
            {
                Log.Warning("[Visual Progression] Couldn't read jobs, so items they make keep their normal art: " + e);
            }
        }
    }
}
