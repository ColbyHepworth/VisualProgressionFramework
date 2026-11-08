using System.Collections.Generic;

namespace VisualProgression
{
    /// <summary>One instruction of a method, reduced to what the scan for made items needs.</summary>
    public readonly struct CodeStep
    {
        private enum StepKind
        {
            Other,
            LoadsItem,
            MakesThing,
        }

        private readonly StepKind kind;
        private readonly string item;

        private CodeStep(StepKind kind, string item)
        {
            this.kind = kind;
            this.item = item;
        }

        /// <summary>Reads a field that holds a listed item.</summary>
        public static CodeStep LoadsItem(string item)
        {
            return new CodeStep(StepKind.LoadsItem, item);
        }

        /// <summary>Calls ThingMaker.MakeThing, or a GenSpawn.Spawn that takes the item's def.</summary>
        public static CodeStep MakesThing => new CodeStep(StepKind.MakesThing, null);

        /// <summary>Any other instruction.</summary>
        public static CodeStep Other => new CodeStep(StepKind.Other, null);

        internal bool IsItemLoad => kind == StepKind.LoadsItem;

        internal bool IsMakeCall => kind == StepKind.MakesThing;

        internal string Item => item;
    }

    /// <summary>Finds the listed items a method makes by naming them in its own code.</summary>
    public static class MadeByName
    {
        /// <summary>
        /// The listed items a method makes by name: each item it reads that a make call follows. The arguments between the
        /// read and the call don't matter, so an item chosen from several before one call counts each of them.
        /// </summary>
        public static IReadOnlyCollection<string> ItemsMadeByName(IEnumerable<CodeStep> steps)
        {
            var made = new HashSet<string>();
            var readSinceLastMake = new List<string>();
            foreach (CodeStep step in steps)
            {
                if (step.IsItemLoad)
                {
                    readSinceLastMake.Add(step.Item);
                }
                else if (step.IsMakeCall)
                {
                    made.UnionWith(readSinceLastMake);
                    readSinceLastMake.Clear();
                }
            }
            return made;
        }
    }
}
