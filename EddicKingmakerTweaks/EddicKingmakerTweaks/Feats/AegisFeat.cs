using System;
using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Controllers.Brain.Blueprints;
using Kingmaker.Controllers.Brain.Blueprints.Considerations;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using UnityEngine;

namespace EddicKingmakerTweaks.Feats
{
    /// <summary>
    /// Aegis: grants a toggleable ability. While active, enemy AI is compelled to
    /// prefer the character as a target over characters without the buff.
    /// </summary>
    internal static class AegisFeat
    {
        // Every blueprint needs a unique, *stable* guid: it is written into save
        // files, so it must never change once the mod is released.
        private const string FeatGuid = "3fd2f132642a4e2486117d641e850ec7";
        private const string BuffGuid = "53b5bad1ebc044ae9deb3e84d729ddb3";
        private const string ToggleGuid = "14e18ad297bd4927b5eb15b237b4a9d6";
        private const string ConsiderationGuid = "a4c62b11ab1d40049237a431074e81ec";

        // Vanilla FightDefensivelyToggleAbility: we borrow its steel-shield icon
        // (we ship no art assets; a null icon renders as a white box on the hotbar).
        private const string FightDefensivelyToggleId = "09d742e8b50b0214fb71acfc99cc00b3";

        private const string DisplayName = "Aegis";
        private const string Description = "Enemies will be compelled to target you instead of your allies when possible.";

        // How attractive targets without Aegis are while an Aegis carrier is a valid
        // alternative. 1 = feat has no effect, 0 = enemies exclusively attack the carrier.
        private const float NonAegisTargetScore = 0.4f;

        public static void Create()
        {
            // Custom mod icon; falls back to the vanilla Fighting Defensively
            // steel shield if the file is missing or unreadable.
            var icon = Helpers.LoadSprite("aegis.png")
                ?? ((BlueprintUnitFact)Main.Library.BlueprintsByAssetId[FightDefensivelyToggleId]).m_Icon;

            // The buff the character carries while the ability is switched on.
            var buff = Helpers.CreateBuff(
                "saga_aegis_buff",
                DisplayName,
                Description,
                BuffGuid,
                icon);

            // The toggle shown on the action bar; the game applies/removes the
            // buff automatically when it is activated/deactivated.
            var toggle = Helpers.CreateToggleAbility(
                "saga_aegis_toggle",
                DisplayName,
                Description,
                ToggleGuid,
                icon,
                buff);

            // Feats use the game's flat two-tone heraldic icon style, distinct
            // from the painterly ability icons; the toggle/buff keep `icon`.
            var feat = Helpers.CreateFeature(
                "saga_aegis",
                DisplayName,
                Description,
                FeatGuid,
                Helpers.LoadSprite("aegis_feat.png") ?? icon,
                FeatureGroup.Feat,
                Helpers.CreateAddFacts(toggle));

            Helpers.AddToFeatSelection(Helpers.BasicFeatSelectionId, feat);

            AddConsiderationToEnemyTargetingActions(buff);
        }

        /// <summary>
        /// The AI scores candidate targets via the Considerations attached to each
        /// brain's AiAction blueprints. Appending our consideration to every action
        /// that targets enemies makes all AI-driven units respect the Aegis buff.
        /// The buff check happens live during scoring, so this is a no-op while
        /// nobody has the toggle active.
        /// </summary>
        private static void AddConsiderationToEnemyTargetingActions(BlueprintBuff buff)
        {
            var consideration = ScriptableObject.CreateInstance<AegisTargetConsideration>();
            consideration.name = "saga_aegis_consideration";
            consideration.AegisBuff = buff;
            consideration.ScoreWithoutAegis = NonAegisTargetScore;
            Helpers.AddAsset(consideration, ConsiderationGuid);

            var enemyActions = Main.Library.GetAllBlueprints()
                .OfType<BlueprintAiAction>()
                .Where(TargetsEnemies)
                .ToList();

            foreach (var action in enemyActions)
            {
                action.TargetConsiderations =
                    (action.TargetConsiderations ?? Array.Empty<Consideration>())
                    .Append(consideration).ToArray();
            }

            Main.Logger.Log($"Aegis consideration added to {enemyActions.Count} AI actions.");
        }

        /// <summary>
        /// The taunt only affects melee/ranged weapon attacks (BlueprintAiAttack covers
        /// both) and touch spells; other spell casts ignore it. Touch spells reach the
        /// AI through BlueprintAiTouch or through BlueprintAiCastSpell with touch range.
        /// </summary>
        private static bool TargetsEnemies(BlueprintAiAction action)
        {
            switch (action)
            {
                case BlueprintAiAttack _:
                case BlueprintAiTouch _:
                    return true;
                case BlueprintAiCastSpell castSpell:
                    return castSpell.Ability != null
                        && castSpell.Ability.CanTargetEnemies
                        && castSpell.Ability.Range == AbilityRange.Touch
                        && (castSpell.Locators == null || castSpell.Locators.Length == 0);
                default:
                    return false;
            }
        }
    }
}
