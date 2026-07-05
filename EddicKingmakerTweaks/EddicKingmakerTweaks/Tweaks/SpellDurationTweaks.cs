using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace EddicKingmakerTweaks.Tweaks
{
    /// <summary>
    /// Extends the duration of Shield and Bless: 1 minute/level + a flat 2 minutes.
    /// Verified against the 2.1.4 blueprint JSON dump: neither ability carries a
    /// ContextRankConfig — their duration rank is the engine default (every rank
    /// slot starts at caster level; RecalculateRanks only overwrites slots that
    /// have a config). So we ADD a config computing caster level + 2 for the
    /// Default rank (BonusValue progression: value + m_StepLevel). Safe because
    /// nothing else on these abilities reads a rank (their buff effects are flat
    /// +4 AC / +1 attack). The static spell description still reads "1 minute/level";
    /// the buff timer gains the flat bonus.
    /// </summary>
    internal static class SpellDurationTweaks
    {
        // Ability blueprints (guids from blueprints.txt). Shield is "MageShield" internally.
        private static readonly (string Name, string Guid)[] ExtendedSpells =
        {
            ("Shield", "ef768022b0785eb43a18969903c537c4"),
            ("Bless", "90e59f4a4ada87243b7b3535a06d0638"),
        };

        public static void Apply()
        {
            foreach (var (name, guid) in ExtendedSpells)
            {
                var ability = (BlueprintAbility)Main.Library.BlueprintsByAssetId[guid];
                if (ability.GetComponents<ContextRankConfig>().Any(c => c.m_Type == AbilityRankType.Default))
                {
                    Main.Logger.Warning($"SpellDurationTweaks: {name} already has a Default rank config — blueprint layout changed, skipped.");
                    continue;
                }

                var config = Helpers.CreateComponent<ContextRankConfig>();
                config.m_Type = AbilityRankType.Default;
                config.m_BaseValueType = ContextRankBaseValueType.CasterLevel;
                config.m_Progression = ContextRankProgression.BonusValue; // rank = caster level + m_StepLevel
                config.m_StepLevel = 2;
                ability.ComponentsArray = ability.ComponentsArray.Append(config).ToArray();
            }
        }
    }
}
