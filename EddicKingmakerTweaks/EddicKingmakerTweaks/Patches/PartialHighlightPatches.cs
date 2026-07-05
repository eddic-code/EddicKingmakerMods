using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers.MapObjects;
using Kingmaker.GameModes;
using Kingmaker.View;

namespace EddicKingmakerTweaks.Patches
{
    /// <summary>
    /// Harmony patches for the partial highlight toggle (adapted from the WotR
    /// PartialHighlightToggle mod). See PartialHighlightManager for the state logic.
    /// </summary>
    [HarmonyPatch]
    internal static class PartialHighlightPatches
    {
        /// <summary>
        /// Vanilla binds HighlightOn/HighlightOff to the highlight key's press and
        /// release. Route both through the manager so releasing the key falls back
        /// to partial highlighting instead of turning everything off.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(InteractionHighlightController), "HighlightOn")]
        private static bool HighlightOn_Prefix(InteractionHighlightController __instance)
        {
            if (!Main.Enabled || __instance.m_Inactive)
                return true;
            PartialHighlightManager.HandleVanillaHighlightOn();
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InteractionHighlightController), "HighlightOff")]
        private static bool HighlightOff_Prefix(InteractionHighlightController __instance)
        {
            if (!Main.Enabled || __instance.m_Inactive)
                return true;
            PartialHighlightManager.HandleVanillaHighlightOff();
            return false;
        }

        private static bool IsSuppressingMode(GameModeType mode)
        {
            return mode == GameModeType.Cutscene
                || mode == GameModeType.CutsceneGlobalMap
                || mode == GameModeType.Dialog;
        }

        /// <summary>
        /// Suppresses the highlight when a cutscene or dialog starts. Must be a
        /// prefix: once the mode has started, the highlight controller is
        /// deactivated and the un-highlight refresh would be ignored.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Game), "DoStartMode")]
        private static void DoStartMode_Prefix(GameModeType type)
        {
            if (Main.Enabled && IsSuppressingMode(type))
                PartialHighlightManager.SetModeSuppressed(true);
        }

        /// <summary>
        /// Restores the highlight when cutscenes/dialogs end. Safe as a postfix:
        /// the resumed mode's controllers (including the highlight controller) are
        /// re-activated before Game calls HandleGameModeChanged.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Game), "HandleGameModeChanged")]
        private static void HandleGameModeChanged_Postfix(GameModeType newMode)
        {
            if (Main.Enabled && !IsSuppressingMode(newMode))
                PartialHighlightManager.SetModeSuppressed(false);
        }

        /// <summary>Suppresses the highlight during combat, restores it afterwards.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameHistoryLog), nameof(GameHistoryLog.HandlePartyCombatStateChanged))]
        private static void HandlePartyCombatStateChanged_Postfix(bool inCombat)
        {
            if (Main.Enabled)
                PartialHighlightManager.SetCombatSuppressed(inCombat);
        }

        /// <summary>Area transitions turn all highlighting off.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.OnAreaLoaded))]
        private static void OnAreaLoaded_Postfix()
        {
            if (Main.Enabled)
                PartialHighlightManager.ResetForNewArea();
        }

        /// <summary>
        /// UnitEntityView.UpdateHighlight decides unit glow via
        /// InteractionHighlightController.IsHighlighting. Swap that read for our
        /// level-aware check so partial mode only highlights lootable corpses.
        /// The controller instance is already on the stack; push `this` (the view)
        /// and call our (controller, view) static instead of the getter.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UnitEntityView), nameof(UnitEntityView.UpdateHighlight))]
        private static IEnumerable<CodeInstruction> UpdateHighlight_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var isHighlightingGetter = AccessTools.PropertyGetter(
                typeof(InteractionHighlightController), nameof(InteractionHighlightController.IsHighlighting));
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(isHighlightingGetter))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(
                        typeof(PartialHighlightManager), nameof(PartialHighlightManager.ShouldHighlightUnit));
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}
