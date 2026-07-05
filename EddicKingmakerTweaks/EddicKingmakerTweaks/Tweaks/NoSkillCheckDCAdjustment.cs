using Kingmaker.Blueprints.GameDifficulties;

namespace EddicKingmakerTweaks.Tweaks
{
    /// <summary>
    /// Zeroes SkillCheckDCBonus in every difficulty stats-adjustment preset
    /// (vanilla: -4 story ... +6 hardest). This hidden DC surcharge is applied
    /// in RuleSkillCheck.OnTrigger / RulePartySkillCheck.IsPassed, but dialogue
    /// *party* checks display the unadjusted DC (SkillCheckResult stores
    /// e.DifficultyClass), so on harder difficulties the dialog UI shows checks
    /// as beating the DC while they secretly fail. Our mod replaces difficulty
    /// scaling with its own level-scaled patches anyway — zeroing the data
    /// removes the surcharge at the source and makes displayed DCs honest for
    /// every preset. (Data-level fix: both rules read the preset live via
    /// BlueprintRoot.DifficultyList.GetAdjustmentPreset.)
    /// </summary>
    internal static class NoSkillCheckDCAdjustment
    {
        // VSDifficultyList (guid from blueprints.txt).
        private const string DifficultyListId = "779631e720352f542bbb425c4b276cce";

        public static void Apply()
        {
            var list = (BlueprintDifficultyList)Main.Library.BlueprintsByAssetId[DifficultyListId];
            foreach (var preset in list.StatsAdjustmentPresets)
                preset.SkillCheckDCBonus = 0;
        }
    }
}
