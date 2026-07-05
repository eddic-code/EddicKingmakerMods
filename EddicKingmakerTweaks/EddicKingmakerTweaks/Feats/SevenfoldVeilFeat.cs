using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;

namespace EddicKingmakerTweaks.Feats
{
    /// <summary>
    /// Sevenfold Veil: a toggleable defensive stance (like Fighting Defensively).
    /// While active: incoming ranged weapon damage is halved, and attacks of
    /// opportunity against you are negated on a successful Mobility check
    /// against the attacker's CMD — the engine-native mechanic behind the
    /// vanilla "Acrobatics (Mobility)" movement mode (UnitCombatState:
    /// UnitCondition.UseMobilityToNegateAttackOfOpportunity rolls
    /// RuleSkillCheck(Mobility) vs RuleCalculateCMD before each AoO). Unlike
    /// the vanilla mode we do NOT also apply the Slowed condition.
    /// </summary>
    internal static class SevenfoldVeilFeat
    {
        // Stable guids, written into save files — never change after release.
        private const string FeatGuid = "be7aa1b430f14e9abc87b200618a093a";
        private const string BuffGuid = "92fa07c57a5f4478aa8a2f92deef07c5";
        private const string ToggleGuid = "30dde71fa86347bdab39663a1d3ae3fa";

        // Vanilla MobilityUseAbilityBuff: icon fallback if our PNG is missing.
        private const string MobilityUseBuffId = "9dc2afb96879cfd4bb7aed475ed51002";

        private const string DisplayName = "Sevenfold Veil";
        private const string Description =
            "You wrap yourself in a shimmering, prismatic veil. While this ability is active, "
            + "damage you take from ranged attacks is halved, and you can move through threatened "
            + "squares without provoking attacks of opportunity by making a Mobility check "
            + "against the enemy's combat maneuver defense.";

        public static void Create()
        {
            var icon = Helpers.LoadSprite("sevenfoldveil.png")
                ?? ((BlueprintUnitFact)Main.Library.BlueprintsByAssetId[MobilityUseBuffId]).m_Icon;

            var mobilityNegatesAoo = Helpers.CreateComponent<AddCondition>();
            mobilityNegatesAoo.Condition = UnitCondition.UseMobilityToNegateAttackOfOpportunity;

            var buff = Helpers.CreateBuff(
                "saga_sevenfold_veil_buff",
                DisplayName,
                Description,
                BuffGuid,
                icon,
                mobilityNegatesAoo,
                Helpers.CreateComponent<SevenfoldVeilRangedProtection>());

            var toggle = Helpers.CreateToggleAbility(
                "saga_sevenfold_veil_toggle",
                DisplayName,
                Description,
                ToggleGuid,
                icon,
                buff);

            // Feats use the game's flat two-tone heraldic icon style, distinct
            // from the painterly ability icons; the toggle/buff keep `icon`.
            var feat = Helpers.CreateFeature(
                "saga_sevenfold_veil",
                DisplayName,
                Description,
                FeatGuid,
                Helpers.LoadSprite("sevenfoldveil_feat.png") ?? icon,
                FeatureGroup.Feat,
                Helpers.CreateAddFacts(toggle));

            Helpers.AddToFeatSelection(Helpers.BasicFeatSelectionId, feat);
        }
    }

    /// <summary>
    /// Halves incoming ranged weapon damage. Same pipeline as Bastion: all
    /// weapon damage flows through RuleDealDamage with Reason.Rule set to the
    /// triggering RuleAttackWithWeapon; we multiply the rule's built-in
    /// Modifier (floored by RuleCalculateDamage). Spells and melee attacks
    /// are unaffected.
    /// </summary>
    public class SevenfoldVeilRangedProtection : RuleTargetLogicComponent<RuleDealDamage>
    {
        private const float DamageMultiplier = 0.5f;

        public override void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            if (!Main.Enabled)
                return;
            if (!(evt.Reason.Rule is RuleAttackWithWeapon attack) || attack.Weapon == null)
                return;
            if (!attack.Weapon.Blueprint.IsRanged)
                return;

            // Multiply rather than assign, in case something else set a modifier.
            evt.Modifier = (evt.Modifier ?? 1f) * DamageMultiplier;
        }

        public override void OnEventDidTrigger(RuleDealDamage evt)
        {
        }
    }
}
