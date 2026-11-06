using RimWorld;
using Verse;

namespace VisualProgression
{
    /// <summary>
    /// Keeps item type icons on the colony's age when a game starts or loads. Without VFE Tribals or Node Research to
    /// hook (<see cref="AgeChangeHooks"/>), it also checks once an in game hour, since the game has no signal for the age
    /// changing.
    /// </summary>
    public class AgeIconsComponent : GameComponent
    {
        public AgeIconsComponent(Game game)
        {
        }

        public override void FinalizeInit()
        {
            AgeIcons.ApplyFor(Faction.OfPlayer.def.techLevel);
        }

        // Again after the game finishes starting or loading: mods like Node Research set the age after FinalizeInit.
        public override void StartedNewGame()
        {
            AgeIcons.ApplyFor(Faction.OfPlayer.def.techLevel);
        }

        public override void LoadedGame()
        {
            AgeIcons.ApplyFor(Faction.OfPlayer.def.techLevel);
        }

        public override void GameComponentTick()
        {
            if (!AgeChangeHooks.AnyHooked && Find.TickManager.TicksGame % GenDate.TicksPerHour == 0)
            {
                AgeIcons.ApplyFor(Faction.OfPlayer.def.techLevel);
            }
        }
    }
}
