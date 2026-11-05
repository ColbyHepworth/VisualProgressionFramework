using Xunit;

namespace VisualProgression.Tests
{
    public class AgeLookTableTests
    {
        // The integer values of the game's tech levels.
        private const int Animal = 1;
        private const int Neolithic = 2;
        private const int Medieval = 3;
        private const int Industrial = 4;
        private const int Spacer = 5;
        private const int Ultra = 6;

        private const int NormalArt = -1;

        [Theory]
        // One row: at or below its age uses it, past it keeps the normal art.
        [InlineData(new[] { Medieval }, Animal, 0)]
        [InlineData(new[] { Medieval }, Medieval, 0)]
        [InlineData(new[] { Medieval }, Industrial, NormalArt)]
        // Several rows: the lowest row that still covers the colony's age.
        [InlineData(new[] { Neolithic, Medieval, Industrial }, Animal, 0)]
        [InlineData(new[] { Neolithic, Medieval, Industrial }, Neolithic, 0)]
        [InlineData(new[] { Neolithic, Medieval, Industrial }, Medieval, 1)]
        [InlineData(new[] { Neolithic, Medieval, Industrial }, Industrial, 2)]
        [InlineData(new[] { Neolithic, Medieval, Industrial }, Spacer, NormalArt)]
        // Ages skipped between rows fall to the next row up.
        [InlineData(new[] { Neolithic, Spacer }, Medieval, 1)]
        [InlineData(new[] { Neolithic, Spacer }, Ultra, NormalArt)]
        // Two rows for one age: the first one wins.
        [InlineData(new[] { Medieval, Medieval }, Medieval, 0)]
        // No rows at all.
        [InlineData(new int[0], Medieval, NormalArt)]
        public void RowFor_ColonyAge_UsesFirstRowCoveringItOrNormalArtPastTheLast(
            int[] upToAges, int colonyAge, int expectedRow)
        {
            int row = AgeLookTable.RowFor(upToAges, colonyAge);

            Assert.Equal(expectedRow, row);
        }
    }
}
