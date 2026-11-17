using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// A meal look's art: a group of pictures each for mixed, meat and vegetarian meals, in that order by name, each
    /// with up to three stack sizes. It picks the picture itself, so a mod that changes how the game's meal graphic
    /// picks can't send a look past the pictures it has.
    /// </summary>
    public class Graphic_MealLook : Graphic_MealVariants
    {
        private const int Groups = 3;
        private const int MostSizesPerGroup = 3;
        private const int MixedGroup = 0;
        private const int MeatGroup = 1;
        private const int VegetarianGroup = 2;

        public override Graphic SubGraphicFor(Thing thing)
        {
            if (thing == null)
            {
                return subGraphics[0];
            }
            int sizesPerGroup = subGraphics.Length / Groups;
            // A folder the three groups don't fit is left to the game's graphic. A mod that changes how that one picks
            // can bring folders laid out its own way.
            if (sizesPerGroup < 1 || sizesPerGroup > MostSizesPerGroup)
            {
                return base.SubGraphicFor(thing);
            }
            return subGraphics[GroupOf(thing) * sizesPerGroup + SizeOf(thing, sizesPerGroup)];
        }

        // The game tells meat and vegetarian meals apart only with Ideology.
        private static int GroupOf(Thing meal)
        {
            if (!ModsConfig.IdeologyActive)
            {
                return MixedGroup;
            }
            switch (FoodUtility.GetFoodKind(meal))
            {
                case FoodKind.Meat:
                    return MeatGroup;
                case FoodKind.NonMeat:
                    return VegetarianGroup;
                default:
                    return MixedGroup;
            }
        }

        // One meal, part of a stack, a full stack. With two sizes, anything above one meal is the second.
        private static int SizeOf(Thing meal, int sizesPerGroup)
        {
            if (sizesPerGroup == 1 || meal.stackCount == 1)
            {
                return 0;
            }
            if (sizesPerGroup == 3 && meal.stackCount == meal.def.stackLimit)
            {
                return 2;
            }
            return 1;
        }
    }
}
