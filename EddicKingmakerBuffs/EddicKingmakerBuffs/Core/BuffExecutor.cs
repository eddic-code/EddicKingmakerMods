using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Utility;
using UnityEngine;

namespace EddicKingmakerBuffs.Core
{
    /// <summary>One planned cast: this buff on this target (null = self-targeted, whoever casts).</summary>
    public class CastTask
    {
        public AvailableBuff Buff;
        public UnitEntityData Target;
        public string PreferredCaster;
    }

    /// <summary>
    /// Executes the configured buff routine: scan, plan, cast.
    /// Casting goes through RuleCastSpell directly (no animations, works out of combat
    /// from anywhere) and spends slots/components like a regular cast would.
    /// </summary>
    public static class BuffExecutor
    {
        public static void ExecuteRoutine()
        {
            var player = Game.Instance?.Player;
            if (player == null || player.Party.Count == 0)
                return;

            var routine = BuffRoutine.LoadForCurrentSave();
            if (routine == null)
            {
                Main.Logger.Log("No buff routine configured for this save. Press Shift+F1 to dump available buffs and create a config template.");
                return;
            }

            if (player.IsInCombat && !routine.AllowInCombat)
            {
                Main.Logger.Log("Buff routine skipped: party is in combat (set AllowInCombat to override).");
                return;
            }

            Main.Logger.Log($"Routine loaded: {routine.Buffs.Count} buff entries (AllowInCombat={routine.AllowInCombat}, OverwriteExisting={routine.OverwriteExisting}).");
            if (routine.Buffs.Count == 0)
            {
                Main.Logger.Log($"Nothing to do — add entries to {BuffRoutine.PathForCurrentSave()}");
                return;
            }

            var available = BuffScanner.ScanParty();
            var availableByName = BuffScanner.ByNormalizedName(available);
            var targetPool = TargetPool(player.Party);
            var tasks = new List<CastTask>();

            foreach (var assignment in routine.Buffs)
            {
                var nameKey = BuffScanner.NormalizeName(assignment.SpellName);
                if (nameKey.Length == 0 || !availableByName.TryGetValue(nameKey, out var buff))
                {
                    Main.Logger.Log($"Skipping \"{assignment.SpellName}\" — no one in the party can currently cast it (check the name against buffs-available.txt).");
                    continue;
                }
                assignment.SpellGuid = buff.AssetGuid;

                // Toggles aren't cast at anyone — they're switched on for the character who has
                // them (pick who via PreferredCaster); the Targets list is ignored.
                if (buff.IsActivatable)
                {
                    Main.Logger.Log($"  {assignment.SpellName} -> {buff.Name} (toggle, {buff.Providers.Count} owner(s)).");
                    tasks.Add(new CastTask { Buff = buff, Target = null, PreferredCaster = assignment.PreferredCaster });
                    continue;
                }

                // No targets listed = cast once, self-targeted. The natural fit for personal-range
                // party buffs like Bless, which are cast on the caster and spread to allies around them.
                if (assignment.Targets.Count == 0)
                {
                    Main.Logger.Log($"  {assignment.SpellName} -> {buff.Name} ({buff.Providers.Count} caster(s), 1 self-targeted cast planned).");
                    tasks.Add(new CastTask { Buff = buff, Target = null, PreferredCaster = assignment.PreferredCaster });
                    continue;
                }

                var targets = ResolveTargets(assignment, targetPool);
                if (targets.Count == 0)
                {
                    Main.Logger.Log($"No matching targets for {buff.Name} (targets: {string.Join(", ", assignment.Targets)}).");
                    continue;
                }

                // Party-wide spells cover everyone with a single cast.
                if (buff.IsMass)
                    targets = targets.Take(1).ToList();

                Main.Logger.Log($"  {assignment.SpellName} -> {buff.Name} ({buff.Providers.Count} caster(s), {targets.Count} cast(s) planned{(buff.IsMass ? ", mass" : "")}).");

                foreach (var target in targets)
                    tasks.Add(new CastTask { Buff = buff, Target = target, PreferredCaster = assignment.PreferredCaster });
            }

            BuffCastController.Instance.Run(tasks, routine.OverwriteExisting);
        }

        /// <summary>Party members plus their pets (pets are valid buff targets, not casters).</summary>
        private static List<UnitEntityData> TargetPool(List<UnitEntityData> party)
        {
            var pool = new List<UnitEntityData>(party);
            foreach (var unit in party)
            {
                var pet = unit.Descriptor.Pet;
                if (pet != null)
                    pool.Add(pet);
            }
            return pool;
        }

        private static List<UnitEntityData> ResolveTargets(BuffAssignment assignment, List<UnitEntityData> pool)
        {
            if (assignment.Targets.Any(t => string.Equals(t, "all", StringComparison.OrdinalIgnoreCase)))
                return new List<UnitEntityData>(pool);

            return pool
                .Where(u => assignment.Targets.Any(t => string.Equals(t, u.CharacterName, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
    }

    /// <summary>Coroutine host so a large routine spreads its casts over a few frames.</summary>
    public class BuffCastController : MonoBehaviour
    {
        private const int CastsPerFrame = 6;

        private static BuffCastController instance;
        private bool running;

        public static BuffCastController Instance
        {
            get
            {
                if (instance == null)
                {
                    var host = new GameObject("EddicKingmakerBuffs.BuffCastController");
                    DontDestroyOnLoad(host);
                    instance = host.AddComponent<BuffCastController>();
                }
                return instance;
            }
        }

        public void Run(List<CastTask> tasks, bool overwriteExisting)
        {
            if (running)
            {
                Main.Logger.Log("Buff routine already running; ignoring.");
                return;
            }
            StartCoroutine(CastRoutine(tasks, overwriteExisting));
        }

        private IEnumerator CastRoutine(List<CastTask> tasks, bool overwriteExisting)
        {
            running = true;
            int applied = 0, skipped = 0, failed = 0, fizzled = 0;

            try
            {
                int inBatch = 0;
                foreach (var task in tasks)
                {
                    if (!Main.Enabled)
                        break;

                    if (task.Target != null && task.Target.Descriptor.State.IsDead)
                    {
                        skipped++;
                        continue;
                    }

                    if (task.Buff.IsActivatable)
                    {
                        switch (TryToggleOn(task))
                        {
                            case CastOutcome.Applied: applied++; break;
                            case CastOutcome.AlreadyActive: skipped++; break;
                            case CastOutcome.NoCaster: failed++; break;
                        }
                        continue;
                    }

                    // Self-targeted casts benefit the whole party; only skip when nobody needs it.
                    bool alreadyActive = task.Target != null
                        ? task.Buff.Effects.IsAlreadyPresentOn(task.Target)
                        : Game.Instance.Player.Party
                            .Where(u => !u.Descriptor.State.IsDead)
                            .All(u => task.Buff.Effects.IsAlreadyPresentOn(u));

                    if (!overwriteExisting && alreadyActive)
                    {
                        skipped++;
                        continue;
                    }

                    switch (TryCast(task))
                    {
                        case CastOutcome.Applied: applied++; break;
                        case CastOutcome.Fizzled: fizzled++; break;
                        case CastOutcome.NoCaster: failed++; break;
                    }

                    if (++inBatch >= CastsPerFrame)
                    {
                        inBatch = 0;
                        yield return null;
                    }
                }
            }
            finally
            {
                running = false;
                Main.Logger.Log($"Buff routine done: {applied} applied, {skipped} already active/skipped, {failed} without caster, {fizzled} fizzled.");
            }
        }

        private enum CastOutcome { Applied, Fizzled, NoCaster, AlreadyActive }

        /// <summary>
        /// Switch on an activatable toggle (bardic performance, stance). The game handles group
        /// exclusivity itself (turning on a song can end another in the same group).
        /// </summary>
        private static CastOutcome TryToggleOn(CastTask task)
        {
            var providers = task.Buff.Providers
                .OrderByDescending(p => !string.IsNullOrEmpty(task.PreferredCaster)
                    && string.Equals(p.Caster.CharacterName, task.PreferredCaster, StringComparison.OrdinalIgnoreCase));

            foreach (var provider in providers)
            {
                if (provider.Activatable.IsOn)
                    return CastOutcome.AlreadyActive;
            }

            foreach (var provider in providers)
            {
                if (provider.Caster.Descriptor.State.IsDead || !provider.Activatable.IsAvailable)
                    continue;

                provider.Activatable.IsOn = true;
                if (provider.Activatable.IsOn)
                    return CastOutcome.Applied;
            }

            Main.Logger.Log($"Could not activate {task.Buff.Name} (out of resources, or restricted right now).");
            return CastOutcome.NoCaster;
        }

        private static CastOutcome TryCast(CastTask task)
        {
            var targetName = task.Target?.CharacterName ?? "the party";

            // Preferred caster first (if named), then everyone else who has the spell.
            var providers = task.Buff.Providers
                .OrderByDescending(p => !string.IsNullOrEmpty(task.PreferredCaster)
                    && string.Equals(p.Caster.CharacterName, task.PreferredCaster, StringComparison.OrdinalIgnoreCase));

            foreach (var provider in providers)
            {
                // Personal-range spells (TargetAnchor.Owner) can only ever target their caster —
                // the game applies party-wide effects itself (e.g. Bless via AbilityTargetsAround).
                var targetUnit = task.Target;
                if (targetUnit == null || provider.SpellToCast.TargetAnchor == AbilityTargetAnchor.Owner)
                    targetUnit = provider.Caster;
                var target = new TargetWrapper(targetUnit);

                if (provider.CastsLeft == 0)
                    continue;
                if (!provider.SpellToCast.CanTarget(target))
                    continue;

                try
                {
                    var rule = new RuleCastSpell(provider.SpellToCast, target);
                    rule.Context.DisableLog = true;
                    Rulebook.Trigger(rule);

                    // A real cast pays its costs whether or not it fizzles (arcane spell failure).
                    provider.SlottedSpell.Spend();

                    if (rule.Success)
                        return CastOutcome.Applied;

                    Main.Logger.Log($"{provider.Caster.CharacterName}'s {task.Buff.Name} on {targetName} fizzled (spell failure).");
                    return CastOutcome.Fizzled;
                }
                catch (Exception e)
                {
                    Main.Logger.Error($"Failed casting {task.Buff.Name} ({provider.Caster.CharacterName} -> {targetName}):");
                    Main.Logger.LogException(e);
                    return CastOutcome.Fizzled;
                }
            }

            Main.Logger.Log($"No caster could cast {task.Buff.Name} on {targetName} (no slots left, or spell can't target them).");
            return CastOutcome.NoCaster;
        }
    }
}
