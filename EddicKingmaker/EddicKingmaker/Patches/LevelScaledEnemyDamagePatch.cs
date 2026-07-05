using System;
using HarmonyLib;
using Kingmaker;
using Kingmaker.RuleSystem.Rules.Damage;

namespace EddicKingmaker.Patches
{
    /// <summary>
    /// Companion to LevelScaledDifficultyPatch: the difficulty's damage-to-party
    /// multiplier (2x on Insane via GameDifficultySettings.DamageToParty) also
    /// scales with the main character's level instead of the selected difficulty:
    /// levels 1-3 enemies deal 1.3x damage, 4-6 deal 1.5x, 7-10 deal 1.8x,
    /// 11+ deal 2x. The in-game setting is ignored while the mod is enabled.
    ///
    /// DamageToParty itself is a serialized settings field (mutating it would
    /// write into the player's difficulty settings), so we replace its single
    /// consumer instead: RuleDealDamage.ApplyDifficultyModifiers, mirroring the
    /// vanilla body with our multiplier swapped into the enemy branch.
    /// </summary>
    [HarmonyPatch(typeof(RuleDealDamage), "ApplyDifficultyModifiers")]
    internal static class LevelScaledEnemyDamagePatch
    {
        private static bool Prefix(RuleDealDamage __instance, int damage, ref int __result)
        {
            if (!Main.Enabled)
                return true;

            var mainCharacter = Game.Instance?.Player?.MainCharacter.Value;

            if (mainCharacter == null)
                return true;

            if (__instance.Initiator.IsPlayerFaction)
                __instance.DifficultyModifier = 1f;
            else if (__instance.Initiator.IsPlayersEnemy)
                __instance.DifficultyModifier = GetDamageMultiplier(
                    mainCharacter.Descriptor.Progression.CharacterLevel);

            __result = damage > 0
                ? Math.Max(1, (int)((float)damage * __instance.DifficultyModifier))
                : 0;

            return false;
        }

        private static float GetDamageMultiplier(int level)
        {
            if (level <= 3)
                return 1.0f;
            if (level <= 6)
                return 1.5f;
            if (level <= 12)
                return 1.8f;
            return 2f;
        }
    }
}
