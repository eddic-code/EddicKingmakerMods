using HarmonyLib;
using Kingmaker.EntitySystem.Entities;

namespace EddicKingmakerTweaks.Patches
{
    /// <summary>
    /// Party moves 1.5x faster outside of combat. BaseSpeedMps is the root of the
    /// movement speed chain (CurrentSpeedMps -> ModifiedSpeedMps -> BaseSpeedMps)
    /// and already special-cases player-faction units out of combat, so scaling it
    /// under the same condition affects exactly that: combat speed reads
    /// Stats.Speed directly and stays vanilla, enemies are untouched, and the
    /// party group speed limit (slowest member) scales consistently.
    /// Runs per frame while units move — keep it allocation-free.
    /// </summary>
    [HarmonyPatch(typeof(UnitEntityData), nameof(UnitEntityData.BaseSpeedMps), MethodType.Getter)]
    internal static class FastOutOfCombatMovementPatch
    {
        private const float SpeedMultiplier = 1.5f;

        private static void Postfix(UnitEntityData __instance, ref float __result)
        {
            if (!Main.Enabled)
                return;

            if (!__instance.IsPlayerFaction || __instance.IsInCombat)
                return;

            __result *= SpeedMultiplier;
        }
    }
}
