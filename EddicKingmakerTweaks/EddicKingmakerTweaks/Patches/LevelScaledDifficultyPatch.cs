using HarmonyLib;
using Kingmaker;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Enums;

namespace EddicKingmakerTweaks.Patches
{
    /// <summary>
    /// Enemy difficulty stat bonuses scale with the main character's level instead
    /// of the selected difficulty (simplified from the LevelScaling mod).
    /// The in-game enemy power setting no longer affects these while the mod is enabled.
    /// Replaces DifficultyStatAdvancement.OnTurnOn; the vanilla OnTurnOff removes
    /// modifiers through the component's private m_*Modifier fields, so we store
    /// ours in the same fields (directly writable thanks to the publicizer).
    /// </summary>
    [HarmonyPatch(typeof(DifficultyStatAdvancement), nameof(DifficultyStatAdvancement.OnTurnOn))]
    internal static class LevelScaledDifficultyPatch
    {
        private const int Tier0LevelThreshold = 3;
        private const int Tier1LevelThreshold = 7;
        private const int Tier2LevelThreshold = 12;
        private const int Tier3LevelThreshold = 16;

        private const int Tier0AbilityBonus = 4;
        private const int Tier0AcBonus = 2;

        private const int Tier1AbilityBonus = 4;
        private const int Tier1AcBonus = 4;

        private const int Tier2AbilityBonus = 8;
        private const int Tier2AcBonus = 6;

        private const int Tier3AbilityBonus = 8;
        private const int Tier3AcBonus = 8;

        private static bool Prefix(DifficultyStatAdvancement __instance)
        {
            if (!Main.Enabled)
                return true;

            var mainCharacter = Game.Instance?.Player?.MainCharacter.Value;

            if (mainCharacter == null)
                return true;

            int level = mainCharacter.Descriptor.Progression.CharacterLevel;
            int abilityBonus;
            int derivedBonus;

            if (level >= Tier3LevelThreshold)
            {
                abilityBonus = Tier3AbilityBonus;
                derivedBonus = Tier3AcBonus;
            }
            else if (level >= Tier2LevelThreshold)
            {
                abilityBonus = Tier2AbilityBonus;
                derivedBonus = Tier2AcBonus;
            }
            else if (level >= Tier1LevelThreshold)
            {
                abilityBonus = Tier1AbilityBonus;
                derivedBonus = Tier1AcBonus;
            }
            else
            {
                abilityBonus = Tier0AbilityBonus;
                derivedBonus = Tier0AcBonus;
            }

            var stats = __instance.Owner.Stats;
            __instance.m_StrengthModifier = stats.Strength.AddModifier(abilityBonus, __instance, ModifierDescriptor.Difficulty);
            __instance.m_ConstitutionModifier = stats.Constitution.AddModifier(abilityBonus, __instance, ModifierDescriptor.Difficulty);
            __instance.m_DexterityModifier = stats.Dexterity.AddModifier(abilityBonus, __instance, ModifierDescriptor.Difficulty);
            __instance.m_IntelligenceModifier = stats.Intelligence.AddModifier(abilityBonus, __instance, ModifierDescriptor.Difficulty);
            __instance.m_WisdomModifier = stats.Wisdom.AddModifier(abilityBonus, __instance, ModifierDescriptor.Difficulty);
            __instance.m_CharismaModifier = stats.Charisma.AddModifier(abilityBonus, __instance, ModifierDescriptor.Difficulty);
            __instance.m_SkillPerceptionModifier = stats.SkillPerception.AddModifier(derivedBonus, __instance, ModifierDescriptor.Difficulty);
            __instance.m_ArmorClassModifier = stats.AC.AddModifier(derivedBonus, __instance, ModifierDescriptor.Difficulty);
            __instance.m_AttackModifier = stats.AdditionalAttackBonus.AddModifier(derivedBonus, __instance, ModifierDescriptor.Difficulty);

            return false;
        }
    }
}
