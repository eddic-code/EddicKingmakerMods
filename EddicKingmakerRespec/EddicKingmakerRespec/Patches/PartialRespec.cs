using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Newtonsoft.Json.Linq;
using EddicKingmakerRespec.Core;

namespace EddicKingmakerRespec.Patches
{
    // Partial respec for story companions: they rebuild from level 0 with free
    // class/archetype choice, but keep their identity — portrait, name, gender,
    // voice, appearance and birthday. The main character and custom companions
    // (mercenaries) are NOT touched: they get the vanilla respec, whose race
    // phase is built around actively re-picking race and gender — locking those
    // flags strands the phase in an incompletable state.
    //
    // Two engine quirks shape the design (see CLAUDE.md):
    // - The controller never works on the unit it was given: its ctor serializes
    //   the unit and builds the UI state on a deserialized preview clone, so
    //   identity data must be on the unit BEFORE the ctor runs (prefix).
    // - ApplyLevelup recreates LevelUpState from scratch and replays all actions
    //   every time the preview is rebuilt, silently dropping actions that fail
    //   Check — so state flags our actions depend on must be re-established in
    //   the LevelUpState ctor itself, not set once from outside.
    internal static class RespecState
    {
        /// <summary>Identity of the unit whose respec is currently in flight.</summary>
        internal static IdentitySnapshot Pending;

        /// <summary>
        /// True for story companions (same test LevelUpState uses): not a merc,
        /// not the main character — the respec vacuum copy of the main character
        /// counts as a clone by blueprint, so it is excluded here too.
        /// </summary>
        internal static bool IsLoreCompanion(UnitDescriptor unit) =>
            !unit.IsCustomCompanion() && !unit.IsMainCharacter && !unit.IsCloneOfMainCharacter;
    }

    /// <summary>
    /// Entry point of every respec: capture the unit's identity before the vacuum
    /// copy is built, and zero the blueprint's class-level floor for the duration
    /// of the call. With the floor at 0 the rebuild target is created at level 0
    /// (AddClassLevels applies nothing and attribute bases fall back to 10 for the
    /// 25-point buy), so story companions re-pick their level-1 class and
    /// archetype exactly like a new character. Only companion blueprints carry a
    /// ClassLevelLimit, so this is a no-op for the main character and mercs.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.RespecCompanion))]
    internal static class Player_RespecCompanion_Patch
    {
        private static ClassLevelLimit s_ZeroedLimit;
        private static int s_SavedLimit;

        private static void Prefix(UnitEntityData unit)
        {
            RespecState.Pending = Main.Enabled ? IdentitySnapshot.From(unit) : null;
            if (!Main.Enabled)
                return;

            Main.Logger.Log($"Respec: starting for {unit.CharacterName} (level {unit.Descriptor.Progression.CharacterLevel}).");
            ClassLevelLimit limit = unit.Blueprint.GetComponent<ClassLevelLimit>();
            if (limit != null && limit.LevelLimit > 0)
            {
                s_ZeroedLimit = limit;
                s_SavedLimit = limit.LevelLimit;
                limit.LevelLimit = 0;
                Main.Logger.Log($"Respec: class-level floor {s_SavedLimit} suspended, rebuilding from level 0.");
            }
        }

        // Blueprint mutation must not outlive the call; a finalizer restores it
        // even if the respec setup throws.
        private static void Finalizer()
        {
            if (s_ZeroedLimit != null)
            {
                s_ZeroedLimit.LevelLimit = s_SavedLimit;
                s_ZeroedLimit = null;
            }
        }
    }

    /// <summary>
    /// Vanilla already locks portrait/race/gender/alignment/name/voice for story
    /// companions in respec mode. The one adjustment needed for the level-0
    /// rebuild: keep race selectable, so the SelectRace action we inject (below)
    /// passes Check every time the controller rebuilds its state — the action
    /// locks the flag back to false each time it applies.
    /// </summary>
    [HarmonyPatch(typeof(LevelUpState), MethodType.Constructor, typeof(UnitDescriptor), typeof(LevelUpState.CharBuildMode))]
    internal static class LevelUpState_Ctor_Patch
    {
        private static void Postfix(LevelUpState __instance)
        {
            if (!Main.Enabled || __instance.Mode != LevelUpState.CharBuildMode.Respec)
                return;
            if (!__instance.IsFirstLevel || !__instance.IsLoreCompanion)
                return;

            if (__instance.Unit.Progression.Race != null)
                __instance.CanSelectRace = true;
        }
    }

    /// <summary>
    /// Identity carry-over for story companions. The prefix stamps the original
    /// identity onto the rebuild target BEFORE the ctor serializes it for the
    /// preview thread, so every preview clone (and the phase-completion checks
    /// that read it) inherits name, gender and the rest. The postfix then selects
    /// the companion's own race: the race phase never runs for them, but a
    /// level-0 rebuild still needs its racial features.
    /// </summary>
    [HarmonyPatch(typeof(LevelUpController), MethodType.Constructor, typeof(UnitDescriptor), typeof(bool), typeof(JToken), typeof(LevelUpState.CharBuildMode))]
    internal static class LevelUpController_Ctor_Patch
    {
        private static void Prefix(UnitDescriptor unit, LevelUpState.CharBuildMode mode)
        {
            IdentitySnapshot pending = RespecState.Pending;
            if (!Main.Enabled || pending == null)
                return;
            if (mode != LevelUpState.CharBuildMode.Respec || !pending.Matches(unit) || !RespecState.IsLoreCompanion(unit))
                return;

            pending.ApplyScalarsTo(unit);
            // Companions have no custom gender; pin their blueprint gender, the
            // race phase UI reports itself incomplete while this is null.
            if (unit.CustomGender == null)
                unit.CustomGender = unit.Gender;
        }

        private static void Postfix(LevelUpController __instance)
        {
            IdentitySnapshot pending = RespecState.Pending;
            if (!Main.Enabled || pending == null)
                return;
            if (__instance.State.Mode != LevelUpState.CharBuildMode.Respec || !pending.Matches(__instance.Unit))
                return;

            ApplyCompanionRace(__instance);
        }

        private static void ApplyCompanionRace(LevelUpController controller)
        {
            LevelUpState state = controller.State;
            if (!state.IsLoreCompanion || state.NextLevel != 1 || !state.CanSelectRace)
                return;
            BlueprintRace race = controller.Unit.Progression.Race;
            if (race == null)
                return;

            // SelectRace adds the racial features (feature selections like the
            // human bonus feat become regular level-up choices) and locks
            // CanSelectRace again on apply.
            if (!controller.SelectRace(race))
            {
                Main.Logger.Log($"Respec: could not re-apply race {race.name} for {controller.Unit.CharacterName}; race phase left open.");
                return;
            }

            // Races with a floating +2 (human, half-elf, half-orc) normally pick
            // the stat in the hidden race phase; put it where the companion's
            // canonical build has it.
            if (state.CanSelectRaceStat)
            {
                AddClassLevels build = controller.Unit.Progression.Features.SelectFactComponents<AddClassLevels>().FirstOrDefault();
                StatType raceStat = build != null ? build.RaceStat : StatType.Constitution;
                controller.SelectRaceStat(raceStat);
                Main.Logger.Log($"Respec: applied race {race.name} (+2 {raceStat}) for {controller.Unit.CharacterName}.");
            }
            else
            {
                Main.Logger.Log($"Respec: applied race {race.name} for {controller.Unit.CharacterName}.");
            }
        }
    }

    /// <summary>
    /// On commit of a story companion, SetupNewCharacher would stamp a default
    /// doll and birthday onto the rebuilt unit (their respec never went through
    /// those phases). Null the doll state so it keeps its hands off, and write
    /// the original identity back right before the unit gets serialized over the
    /// old one. The main character's commit is left alone — their doll and
    /// identity come from the choices made in the vanilla phases.
    /// </summary>
    [HarmonyPatch(typeof(LevelUpController), nameof(LevelUpController.Commit))]
    internal static class LevelUpController_Commit_Patch
    {
        private static void Prefix(LevelUpController __instance)
        {
            IdentitySnapshot pending = RespecState.Pending;
            if (!Main.Enabled || pending == null)
                return;
            if (__instance.State.Mode != LevelUpState.CharBuildMode.Respec || !pending.Matches(__instance.Unit))
                return;
            if (!__instance.State.IsLoreCompanion)
                return;

            __instance.Doll = null;
            __instance.Unit.Doll = pending.Doll;
            __instance.BirthDay = pending.BirthDay;
            __instance.BirthMonth = pending.BirthMonth;
            pending.ApplyScalarsTo(__instance.Unit);
            if (__instance.Unit.CustomGender == null)
                __instance.Unit.CustomGender = __instance.Unit.Gender;
            Main.Logger.Log($"Respec: committing rebuild of {__instance.Unit.CharacterName}.");
        }

        private static void Postfix(LevelUpController __instance)
        {
            if (RespecState.Pending != null && RespecState.Pending.Matches(__instance.Unit))
                RespecState.Pending = null;
        }
    }

    /// <summary>Drop the snapshot if the player backs out of the respec.</summary>
    [HarmonyPatch(typeof(LevelUpController), nameof(LevelUpController.Cancel))]
    internal static class LevelUpController_Cancel_Patch
    {
        private static void Postfix(LevelUpController __instance)
        {
            if (RespecState.Pending != null && RespecState.Pending.Matches(__instance.Unit))
                RespecState.Pending = null;
        }
    }
}
