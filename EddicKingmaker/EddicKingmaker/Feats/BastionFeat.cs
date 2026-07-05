using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;

namespace EddicKingmaker.Feats
{
    /// <summary>
    /// Bastion: passive feat that reduces incoming melee and ranged weapon damage
    /// (but not spell damage) by 25%, rounded up.
    /// </summary>
    internal static class BastionFeat
    {
        // Stable guid, written into save files — never change after release.
        private const string FeatGuid = "8090efb1dcde4b77b81784b5850d3aa4";

        // Vanilla Stoneskin spell: we borrow its icon — the game's classic
        // "reduce physical damage" art, and distinct from other feat icons.
        private const string StoneskinAbilityId = "c66e86905f7606c4eaa5c774f0357b2b";

        private const string DisplayName = "Bastion";
        private const string Description =
            "You shrug off physical punishment. Damage you take from melee and ranged attacks "
            + "is reduced by 25%. Damage from spells is not affected.";

        public static void Create()
        {
            // Bastion is a single passive feature, so its one icon uses the flat
            // two-tone heraldic feat style; fallback chain: painterly custom
            // icon, then the vanilla Stoneskin icon.
            var icon = Helpers.LoadSprite("bastion_feat.png")
                ?? Helpers.LoadSprite("bastion.png")
                ?? ((BlueprintUnitFact)Main.Library.BlueprintsByAssetId[StoneskinAbilityId]).m_Icon;

            var feat = Helpers.CreateFeature(
                "saga_bastion",
                DisplayName,
                Description,
                FeatGuid,
                icon,
                FeatureGroup.Feat,
                Helpers.CreateComponent<BastionDamageReduction>());

            Helpers.AddToFeatSelection(Helpers.BasicFeatSelectionId, feat);
        }
    }

    /// <summary>
    /// The passive effect: hooks incoming RuleDealDamage on the feat's owner and
    /// multiplies weapon-attack damage by 0.75 via the rule's built-in Modifier
    /// (the game floors the final value, and floor(0.75 * damage) is exactly
    /// "reduce by 25% rounded up"). Spells never come through RuleAttackWithWeapon,
    /// so they are unaffected.
    /// </summary>
    public class BastionDamageReduction : RuleTargetLogicComponent<RuleDealDamage>
    {
        private const float DamageMultiplier = 0.75f;

        public override void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            if (!Main.Enabled || !(evt.Reason.Rule is RuleAttackWithWeapon))
                return;

            // Multiply rather than assign, in case something else set a modifier.
            evt.Modifier = (evt.Modifier ?? 1f) * DamageMultiplier;
        }

        public override void OnEventDidTrigger(RuleDealDamage evt)
        {
        }
    }
}
