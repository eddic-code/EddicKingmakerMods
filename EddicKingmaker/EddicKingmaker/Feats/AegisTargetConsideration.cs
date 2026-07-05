using Kingmaker.Controllers.Brain;
using Kingmaker.Controllers.Brain.Blueprints.Considerations;
using Kingmaker.UnitLogic.Buffs.Blueprints;

namespace EddicKingmaker.Feats
{
    /// <summary>
    /// AI target consideration implementing the Aegis taunt. Consideration scores
    /// multiply into the AI's target score (0..1 range), so a target can't be boosted
    /// above normal — instead, every target *without* the Aegis buff is penalized,
    /// but only while a unit *with* the buff is among the candidate targets.
    /// This way the AI prefers the Aegis carrier, and behaves completely normally
    /// when nobody has the buff active.
    /// </summary>
    public class AegisTargetConsideration : Consideration
    {
        public BlueprintBuff AegisBuff;

        /// <summary>Score for targets without the buff while a carrier is available (1 = no effect, 0 = never target).</summary>
        public float ScoreWithoutAegis = 0.4f;

        public override float Score(DecisionContext context)
        {
            if (!Main.Enabled)
                return 1f;

            var target = context.Target.Unit ?? context.Unit;
            if (target.Buffs.HasFact(AegisBuff))
                return 1f;

            foreach (var candidate in context.Enemies)
            {
                if (candidate.Unit != target && candidate.Unit.Buffs.HasFact(AegisBuff))
                    return ScoreWithoutAegis;
            }
            return 1f;
        }
    }
}
