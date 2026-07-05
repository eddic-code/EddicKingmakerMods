using System.Collections.Generic;
using Kingmaker;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.Log;
using UnityEngine;

namespace EddicKingmakerLoot.Core
{
    /// <summary>Writes messages to the in-game log window (the battle/combat log).</summary>
    public static class GameLog
    {
        /// <summary>
        /// Adds a line to the in-game log window. No-op while the log UI doesn't exist
        /// (main menu, loading screens).
        /// </summary>
        /// <param name="text">The message. Supports TextMeshPro rich text tags.</param>
        /// <param name="color">Text color; vanilla default log color when null.</param>
        /// <param name="tooltip">Optional hover tooltip: an ItemEntity shows the full item card
        /// (pass a copy via ItemsEntityFactory.CreateItemCopy, like vanilla loot lines do),
        /// a string shows plain text.</param>
        public static void Message(string text, Color? color = null, object tooltip = null)
        {
            var manager = Game.Instance?.UI?.BattleLogManager;
            if (manager == null || manager.LogView == null)
                return;

            manager.LogView.AddLogEntry(new LogItemData(
                text,
                color ?? (Color)GameLogStrings.Instance.DefaultColor,
                tooltip,
                PrefixIcon.None,
                new List<LogChannel> { LogChannel.None }));
        }

        /// <summary>
        /// Shows the centered on-screen warning notification and also adds it to the log —
        /// for messages the player must not miss.
        /// </summary>
        public static void Notify(string text)
        {
            EventBus.RaiseEvent<IWarningNotificationUIHandler>(h => h.HandleWarning(text, true));
        }
    }
}
