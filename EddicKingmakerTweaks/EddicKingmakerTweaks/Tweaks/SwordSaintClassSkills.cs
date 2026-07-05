using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;

namespace EddicKingmakerTweaks.Tweaks
{
    /// <summary>
    /// Adds Perception and Mobility to the Sword Saint archetype's class skills.
    /// Archetype skills only apply when ReplaceClassSkills is set, in which case
    /// they replace the parent class's list entirely (see ApplyClassMechanics in
    /// the game code) — so we enable the flag and provide the full list: whatever
    /// the archetype/class already had, plus the two new skills.
    /// </summary>
    internal static class SwordSaintClassSkills
    {
        // Magus class blueprint (guid from the EldritchArcana reference mod).
        private const string MagusClassId = "45a4607686d96a1498891b3286121780";

        // SwordSaintArchetype (guid from our blueprints.txt dump).
        private const string SwordSaintArchetypeId = "7d6678f2160018049814838af2ab4236";

        public static void Apply()
        {
            var magus = (BlueprintCharacterClass)Main.Library.BlueprintsByAssetId[MagusClassId];
            var swordSaint = (BlueprintArchetype)Main.Library.BlueprintsByAssetId[SwordSaintArchetypeId];

            var baseSkills = swordSaint.ReplaceClassSkills ? swordSaint.ClassSkills : magus.ClassSkills;
            swordSaint.ReplaceClassSkills = true;
            swordSaint.ClassSkills = baseSkills
                .Union(new[] { StatType.SkillPerception, StatType.SkillMobility })
                .ToArray();
        }
    }
}
