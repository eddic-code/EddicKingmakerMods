using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.ActivatableAbilities;

namespace EddicKingmakerBuffs.Core
{
    /// <summary>
    /// One way to get a buff up: a spellbook spell, a class ability (spell-like), or an
    /// activatable toggle (bardic performance, Power Attack, ...) on a specific caster.
    /// </summary>
    public class BuffProvider
    {
        public UnitEntityData Caster;

        /// <summary>Set for spellbook spells only.</summary>
        public Spellbook Book;

        /// <summary>The book/fact-level spell. Spend/availability run against this. Null for toggles.</summary>
        public AbilityData SlottedSpell;

        /// <summary>What is actually cast: variant- and touch-resolved. Null for toggles.</summary>
        public AbilityData SpellToCast;

        /// <summary>Set for activatable toggles; SlottedSpell/SpellToCast are null then.</summary>
        public ActivatableAbility Activatable;

        /// <summary>
        /// Casts (or resource rounds) left; -1 means at will. Unlike WotR, Kingmaker's
        /// GetAvailableForCastSpellCount has no cantrip sentinel (it reports 0 and the game
        /// never slot-checks cantrips), so treat cantrips as at-will ourselves.
        /// </summary>
        public int CastsLeft
        {
            get
            {
                if (Activatable != null)
                    return Activatable.ResourceCount ?? -1;
                if (Book != null)
                    return SlottedSpell.Blueprint.IsCantrip ? -1 : Book.GetAvailableForCastSpellCount(SlottedSpell);
                return SlottedSpell.GetAvailableForCastCount();
            }
        }
    }

    /// <summary>A party-castable spell/ability/toggle that applies at least one beneficial buff.</summary>
    public class AvailableBuff
    {
        public string AssetGuid;
        public string Name;
        public AppliedEffects Effects;
        public bool IsMass;
        public bool IsActivatable;
        public readonly List<BuffProvider> Providers = new List<BuffProvider>();
    }

    /// <summary>
    /// Scans the party for buff sources: spellbook spells, class abilities (spell-like,
    /// e.g. domain powers), and activatable toggles (bardic performance, stances).
    /// Not yet scanned: scrolls/wands/potions.
    /// </summary>
    public static class BuffScanner
    {
        /// <summary>
        /// Lookup key for matching user-typed spell names: lowercase, spaces stripped, and
        /// bracketed tags removed so names pasted straight from the dump ("Bless [mass]") match.
        /// </summary>
        public static string NormalizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";
            name = System.Text.RegularExpressions.Regex.Replace(name, @"\[[^\]]*\]", "");
            return name.Replace(" ", "").ToLowerInvariant();
        }

        /// <summary>Available buffs keyed by normalized display name (first scanned wins on collisions).</summary>
        public static Dictionary<string, AvailableBuff> ByNormalizedName(Dictionary<string, AvailableBuff> availableByGuid)
        {
            var byName = new Dictionary<string, AvailableBuff>();
            foreach (var buff in availableByGuid.Values)
            {
                var key = NormalizeName(buff.Name);
                if (key.Length > 0 && !byName.ContainsKey(key))
                    byName[key] = buff;
            }
            return byName;
        }

        /// <summary>Available buffs keyed by the concrete (variant-resolved) spell blueprint guid.</summary>
        public static Dictionary<string, AvailableBuff> ScanParty()
        {
            var result = new Dictionary<string, AvailableBuff>();

            foreach (var unit in Game.Instance.Player.Party)
            {
                foreach (var book in unit.Descriptor.Spellbooks)
                {
                    // Cantrips are castable at will for both prepared and spontaneous books.
                    foreach (var spell in book.GetKnownSpells(0))
                        Add(result, unit, book, spell);

                    if (book.Blueprint.Spontaneous)
                    {
                        for (int level = 1; level <= book.MaxSpellLevel; level++)
                        {
                            foreach (var spell in book.GetKnownSpells(level))
                                Add(result, unit, book, spell);
                            foreach (var spell in book.GetCustomSpells(level))
                                Add(result, unit, book, spell);
                        }
                    }
                    else
                    {
                        // Prepared casters: whatever is memorized right now (includes metamagic customs).
                        foreach (var slot in book.GetAllMemorizedSpells())
                        {
                            if (slot.Spell != null)
                                Add(result, unit, book, slot.Spell);
                        }
                    }
                }

                // Class abilities (spell-like: domain powers etc.). Charge-based item abilities
                // (wands/scrolls) are a later milestone; abilities from worn gear are fine.
                foreach (var fact in unit.Descriptor.Abilities.RawFacts.OfType<Ability>())
                {
                    if (fact.SourceItem != null && fact.SourceItem.IsSpendCharges)
                        continue;
                    Add(result, unit, null, fact.Data);
                }

                // Activatable toggles that apply a buff (bardic performance, stances, judgments).
                foreach (var fact in unit.Descriptor.ActivatableAbilities.RawFacts.OfType<ActivatableAbility>())
                {
                    if (fact.Blueprint.Buff == null || fact.Blueprint.IsTargeted)
                        continue;
                    AddActivatable(result, unit, fact);
                }
            }

            return result;
        }

        private static void AddActivatable(Dictionary<string, AvailableBuff> result, UnitEntityData unit, ActivatableAbility fact)
        {
            var guid = fact.Blueprint.AssetGuid;

            if (!result.TryGetValue(guid, out var buff))
            {
                buff = new AvailableBuff
                {
                    AssetGuid = guid,
                    Name = fact.Blueprint.Name,
                    Effects = new AppliedEffects(),
                    IsActivatable = true,
                };
                result[guid] = buff;
            }

            buff.Providers.Add(new BuffProvider { Caster = unit, Activatable = fact });
        }

        private static void Add(Dictionary<string, AvailableBuff> result, UnitEntityData unit, Spellbook book, AbilityData spell)
        {
            // Variant parents (e.g. Protection from Alignment) are not castable themselves;
            // expand to their concrete variants, each spending the same slotted spell.
            if (spell.Blueprint.HasVariants)
            {
                foreach (var variant in spell.Blueprint.Variants)
                    AddConcrete(result, unit, book, spell, new AbilityData(spell, variant));
                return;
            }

            AddConcrete(result, unit, book, spell, spell);
        }

        private static void AddConcrete(Dictionary<string, AvailableBuff> result, UnitEntityData unit, Spellbook book, AbilityData slotted, AbilityData concrete)
        {
            var guid = concrete.Blueprint.AssetGuid;

            if (!result.TryGetValue(guid, out var buff))
            {
                var effects = AppliedEffects.Of(concrete.Blueprint);
                if (effects.Empty)
                    return; // Applies no beneficial buff — not our business.

                buff = new AvailableBuff
                {
                    AssetGuid = guid,
                    Name = concrete.Name,
                    Effects = effects,
                    IsMass = concrete.Blueprint.IsMass(),
                };
                result[guid] = buff;
            }

            // One provider per caster+book+spell; duplicate memorized copies just raise CastsLeft.
            foreach (var existing in buff.Providers)
            {
                if (existing.Caster == unit && existing.Book == book && existing.SlottedSpell.Blueprint == slotted.Blueprint)
                    return;
            }

            // AbilityEffectStickyTouch parents error out when cast directly; cast the touch child instead.
            var touch = concrete.Blueprint.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectStickyTouch>();
            var toCast = touch != null && touch.TouchDeliveryAbility != null
                ? new AbilityData(concrete, touch.TouchDeliveryAbility)
                : concrete;

            buff.Providers.Add(new BuffProvider
            {
                Caster = unit,
                Book = book,
                SlottedSpell = slotted,
                SpellToCast = toCast,
            });
        }
    }
}
