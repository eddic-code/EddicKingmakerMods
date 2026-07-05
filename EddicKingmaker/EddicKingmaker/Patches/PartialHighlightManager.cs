using Kingmaker;
using Kingmaker.Controllers.MapObjects;
using Kingmaker.PubSubSystem;
using Kingmaker.View;

namespace EddicKingmaker.Patches
{
    /// <summary>
    /// State and refresh logic for the partial highlight toggle (adapted from the
    /// WotR PartialHighlightToggle mod). Two highlight levels exist:
    /// - Full: the vanilla hold-TAB behavior, highlights everything.
    /// - Partial: our toggle, highlights only interactable map objects (chests,
    ///   doors, levers, ...) and lootable corpses; living units stay unhighlighted
    ///   via the transpiler in PartialHighlightPatches.
    /// The partial highlight is suppressed (not cleared) during cutscenes, dialogs
    /// and combat, and restored when they end. Area transitions clear everything.
    /// </summary>
    internal static class PartialHighlightManager
    {
        /// <summary>The user's toggle. Survives suppression; cleared on area transitions.</summary>
        public static bool PartialToggledOn;

        /// <summary>Vanilla highlight key currently held down.</summary>
        private static bool _fullHighlightOn;

        /// <summary>In a cutscene or dialog.</summary>
        private static bool _modeSuppressed;

        /// <summary>Party is in combat.</summary>
        private static bool _combatSuppressed;

        private static bool PartialActive => PartialToggledOn && !_modeSuppressed && !_combatSuppressed;

        public static void Toggle()
        {
            PartialToggledOn = !PartialToggledOn;
            Refresh();
        }

        public static void HandleVanillaHighlightOn()
        {
            _fullHighlightOn = true;
            Refresh();
        }

        public static void HandleVanillaHighlightOff()
        {
            _fullHighlightOn = false;
            Refresh();
        }

        public static void SetModeSuppressed(bool value)
        {
            if (_modeSuppressed == value)
                return;
            _modeSuppressed = value;
            Refresh();
        }

        public static void SetCombatSuppressed(bool value)
        {
            if (_combatSuppressed == value)
                return;
            _combatSuppressed = value;
            Refresh();
        }

        /// <summary>
        /// Area transitions drop all highlight state. The controller may already be
        /// deactivated here, so force the flag off directly instead of refreshing;
        /// the new area's views compute their highlight from scratch anyway.
        /// </summary>
        public static void ResetForNewArea()
        {
            PartialToggledOn = false;
            _fullHighlightOn = false;
            _modeSuppressed = false;
            _combatSuppressed = false;

            var game = Game.Instance;
            var controller = game?.InteractionHighlightController;
            if (controller != null)
            {
                controller.IsHighlighting = false;
                game.IsTempHighlight = false;
            }
        }

        /// <summary>Whether a specific unit should glow, given the current highlight level.</summary>
        public static bool ShouldHighlightUnit(InteractionHighlightController controller, UnitEntityView view)
        {
            if (_fullHighlightOn)
                return controller.IsHighlighting;
            return PartialActive && view?.EntityData?.IsDeadAndHasLoot == true;
        }

        /// <summary>Re-applies highlight state to the world, mirroring the vanilla controller's loops.</summary>
        public static void Refresh()
        {
            var controller = Game.Instance?.InteractionHighlightController;
            if (controller == null || controller.m_Inactive)
                return;

            bool highlighting = _fullHighlightOn || PartialActive;
            controller.IsHighlighting = highlighting;
            Game.Instance.IsTempHighlight = highlighting;

            foreach (var mapObject in Game.Instance.State.MapObjects)
                mapObject.View?.UpdateHighlight();
            foreach (var unit in Game.Instance.State.Units)
                unit.View?.UpdateHighlight(false);

            EventBus.RaiseEvent<IInteractionHighlightUIHandler>(h => h.HandleHighlightChange(highlighting));
        }
    }
}
