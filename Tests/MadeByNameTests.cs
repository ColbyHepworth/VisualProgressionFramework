using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace VisualProgression.Tests
{
    public class MadeByNameTests
    {
        [Theory]
        // Nothing to read.
        [InlineData("", "")]
        // MakeThing(DefOf.Pack): the field, the null stuff, the call.
        [InlineData("Pack other make", "Pack")]
        // GenSpawn.Spawn(DefOf.Pack, cell, map, mode): several arguments between the field and the call.
        [InlineData("Pack other other other make", "Pack")]
        // Read but never made, such as a comparison with a pawn's def.
        [InlineData("Pack other other", "")]
        // Makes something that isn't listed.
        [InlineData("other other make", "")]
        // Read only after the make call, such as checking what was made.
        [InlineData("other make Pack other", "")]
        [InlineData("PackA other make PackB other", "PackA")]
        // Two make calls, each with its own item.
        [InlineData("PackA other make PackB other make", "PackA,PackB")]
        // A choice between two items before one make call keeps both.
        [InlineData("PackA other PackB other other make", "PackA,PackB")]
        public void ItemsMadeByName_MethodSteps_EachItemReadBeforeAMakeCall(string method, string expectedItems)
        {
            IReadOnlyCollection<string> items = MadeByName.ItemsMadeByName(Steps(method));

            Assert.Equal(ItemList(expectedItems), items.Distinct().OrderBy(item => item, StringComparer.Ordinal));
        }

        /// Turns "PackA other make" into steps: "make" is a make call, "other" any other instruction,
        /// and any other word reads the listed item of that name.
        private static List<CodeStep> Steps(string method)
        {
            var steps = new List<CodeStep>();
            foreach (string word in method.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (word == "make")
                {
                    steps.Add(CodeStep.MakesThing);
                }
                else if (word == "other")
                {
                    steps.Add(CodeStep.Other);
                }
                else
                {
                    steps.Add(CodeStep.LoadsItem(word));
                }
            }
            return steps;
        }

        private static IEnumerable<string> ItemList(string commaSeparated)
        {
            return commaSeparated
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .OrderBy(item => item, StringComparer.Ordinal);
        }
    }
}
