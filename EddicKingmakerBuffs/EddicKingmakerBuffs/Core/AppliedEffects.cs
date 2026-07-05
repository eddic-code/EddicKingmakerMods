using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.GenericSlot;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;

namespace EddicKingmakerBuffs.Core
{
    /// <summary>
    /// The buff/enchantment blueprints a spell applies, extracted from its action tree.
    /// Ported from BubbleBuffs' GetBeneficialBuffs/AbilityCombinedEffects (WotR), trimmed to
    /// what exists in Kingmaker. Guids are strings because Kingmaker's AssetGuid is a string.
    /// </summary>
    public class AppliedEffects
    {
        public readonly HashSet<string> Buffs = new HashSet<string>();
        public readonly HashSet<string> PetBuffs = new HashSet<string>();
        public readonly HashSet<string> PrimaryHandEnchants = new HashSet<string>();
        public readonly HashSet<string> SecondaryHandEnchants = new HashSet<string>();

        /// <summary>True if any applied effect outlasts round-scale durations.</summary>
        public bool IsLong { get; private set; }

        public bool Empty =>
            Buffs.Count == 0 && PetBuffs.Count == 0 &&
            PrimaryHandEnchants.Count == 0 && SecondaryHandEnchants.Count == 0;

        public static AppliedEffects Of(BlueprintAbility spell)
        {
            var result = new AppliedEffects();
            var runAction = spell.DeTouchify().GetComponent<AbilityEffectRunAction>();
            if (runAction?.Actions?.Actions != null)
            {
                foreach (var action in runAction.Actions.Actions)
                    result.Collect(action, onPet: false);
            }
            return result;
        }

        private void Collect(GameAction action, bool onPet)
        {
            switch (action)
            {
                case null:
                    return;

                case ContextActionApplyBuff applyBuff when applyBuff.Buff != null && applyBuff.Buff.IsBeneficial():
                    (onPet ? PetBuffs : Buffs).Add(applyBuff.Buff.AssetGuid);
                    IsLong |= applyBuff.Permanent
                        || (applyBuff.UseDurationSeconds && applyBuff.DurationSeconds >= 60)
                        || applyBuff.DurationValue?.Rate != DurationRate.Rounds;
                    return;

                case ContextActionsOnPet onPetAction:
                    CollectAll(onPetAction.Actions?.Actions, onPet: true);
                    return;

                case ContextActionEnchantWornItem enchant when enchant.Enchantment != null:
                    if (enchant.Slot == EquipSlotBase.SlotType.PrimaryHand)
                        PrimaryHandEnchants.Add(enchant.Enchantment.AssetGuid);
                    else if (enchant.Slot == EquipSlotBase.SlotType.SecondaryHand)
                        SecondaryHandEnchants.Add(enchant.Enchantment.AssetGuid);
                    IsLong |= enchant.Permanent || enchant.DurationValue?.Rate != DurationRate.Rounds;
                    return;

                case ContextActionPartyMembers party:
                    CollectAll(party.Action?.Actions, onPet);
                    return;

                case ContextActionSpawnAreaEffect spawnArea when spawnArea.AreaEffect != null:
                {
                    var areaBuff = spawnArea.AreaEffect.GetComponent<AbilityAreaEffectBuff>();
                    if (areaBuff?.Buff != null && areaBuff.Buff.IsBeneficial())
                    {
                        (onPet ? PetBuffs : Buffs).Add(areaBuff.Buff.AssetGuid);
                        IsLong |= spawnArea.DurationValue?.Rate != DurationRate.Rounds;
                    }
                    return;
                }

                case Conditional conditional:
                {
                    // Take both branches unless an is-ally condition clearly marks one as hostile-only.
                    bool takeTrue = true, takeFalse = true;
                    foreach (var condition in conditional.ConditionsChecker.Conditions.OfType<ContextConditionIsAlly>())
                    {
                        if (condition.Not) takeTrue = false;
                        else takeFalse = false;
                    }
                    if (takeTrue) CollectAll(conditional.IfTrue?.Actions, onPet);
                    if (takeFalse) CollectAll(conditional.IfFalse?.Actions, onPet);
                    return;
                }

                case ContextActionCastSpell castSpell when castSpell.Spell != null:
                {
                    var runAction = castSpell.Spell.DeTouchify().GetComponent<AbilityEffectRunAction>();
                    CollectAll(runAction?.Actions?.Actions, onPet);
                    return;
                }
            }
        }

        private void CollectAll(IEnumerable<GameAction> actions, bool onPet)
        {
            if (actions == null)
                return;
            foreach (var action in actions)
                Collect(action, onPet);
        }

        /// <summary>Skip-check: is any effect of this spell already active on the target (or its pet/weapons)?</summary>
        public bool IsAlreadyPresentOn(UnitEntityData target)
        {
            if (Buffs.Count > 0 && target.Descriptor.Buffs.RawFacts.Any(b => Buffs.Contains(b.Blueprint.AssetGuid)))
                return true;

            var pet = target.Descriptor.Pet;
            if (PetBuffs.Count > 0 && pet != null && pet.Descriptor.Buffs.RawFacts.Any(b => PetBuffs.Contains(b.Blueprint.AssetGuid)))
                return true;

            var primary = target.Body.PrimaryHand.MaybeWeapon;
            if (PrimaryHandEnchants.Count > 0 && primary != null &&
                primary.Enchantments.Any(e => PrimaryHandEnchants.Contains(e.Blueprint.AssetGuid)))
                return true;

            var secondary = target.Body.SecondaryHand.MaybeWeapon;
            if (SecondaryHandEnchants.Count > 0 && secondary != null &&
                secondary.Enchantments.Any(e => SecondaryHandEnchants.Contains(e.Blueprint.AssetGuid)))
                return true;

            return false;
        }
    }

    public static class SpellExtensions
    {
        /// <summary>Touch spells wrap the real effect in a delivery child ability; unwrap it.</summary>
        public static BlueprintAbility DeTouchify(this BlueprintAbility spell)
        {
            var touch = spell.GetComponent<AbilityEffectStickyTouch>();
            return touch != null && touch.TouchDeliveryAbility != null ? touch.TouchDeliveryAbility : spell;
        }

        /// <summary>A buff is "beneficial" unless applying it forces a saving throw (debuff-shaped).</summary>
        public static bool IsBeneficial(this BlueprintBuff buff)
        {
            var contextActions = buff.GetComponent<AddFactContextActions>();
            var activated = contextActions?.Activated?.Actions;
            return activated == null || !activated.Any(a => a is ContextActionSavingThrow);
        }

        /// <summary>Party-wide spells: cast once, not once per target.</summary>
        public static bool IsMass(this BlueprintAbility spell)
        {
            spell = spell.DeTouchify();
            if (spell.GetComponent<AbilityTargetsAround>() != null)
                return true;
            var runAction = spell.GetComponent<AbilityEffectRunAction>();
            return runAction?.Actions?.Actions != null
                && runAction.Actions.Actions.Any(a => a is ContextActionSpawnAreaEffect || a is ContextActionPartyMembers);
        }
    }
}
