using HarmonyLib;
using Kingmaker.RuleSystem.Rules;

namespace EddicKingmakerTweaks.Patches
{
    /// <summary>
    /// "Take 10" outside of combat (pattern from BagOfTricks' d20 patches): any d20
    /// rolled by a player-faction unit that is not in combat yields at least 10.
    /// Covers skill checks, perception rolls, saves from traps, etc.
    /// </summary>
    [HarmonyPatch(typeof(RuleRollD20), "Roll")]
    internal static class MinimumD20Patch
    {
        private const int MinimumRoll = 10;

        private static void Postfix(RuleRollD20 __instance, ref int __result)
        {
            if (!Main.Enabled)
                return;
            var initiator = __instance.Initiator;
            if (initiator == null || !initiator.IsPlayerFaction || initiator.IsInCombat)
                return;
            if (__result < MinimumRoll)
                __result = MinimumRoll;
        }
    }
}
