using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.PubSubSystem;

namespace EddicKingmakerRespec.Core
{
    internal static class RespecService
    {
        /// <summary>
        /// Opens the vanilla respec character selector with all eligible party
        /// members (in party or at the capital). Picking one starts the game's
        /// own respec flow; unlike the storyteller version this charges no gold
        /// and advances no game time.
        /// </summary>
        public static void OpenCharacterSelector()
        {
            Game game = Game.Instance;
            if (game?.Player?.MainCharacter.Value == null || game.State.LoadedAreaState == null)
                return;
            if (game.CurrentMode != GameModeType.Default && game.CurrentMode != GameModeType.Pause)
                return;

            // Same pool as the vanilla RespecCompanion action, but without its
            // class-level floor: we rebuild story companions from level 0, so any
            // living, leveled non-pet qualifies.
            List<UnitEntityData> units = game.Player.PartyCharacters
                .Concat(game.Player.RemoteCompanions)
                .Select(r => r.Value)
                .Where(u => u != null && !u.IsInCombat
                    && !u.Descriptor.State.IsFinallyDead
                    && !u.Descriptor.IsPet
                    && u.Descriptor.Progression.CharacterLevel > 0)
                .ToList();

            if (units.Count == 0)
            {
                Main.Logger.Log("No characters available for respec.");
                return;
            }

            EventBus.RaiseEvent<ICharacterSelectorHandler>(h => h.HandleSelectCharacter(units, OnRespecCommitted));
        }

        private static void OnRespecCommitted() => Main.Logger.Log("Respec committed.");
    }
}
