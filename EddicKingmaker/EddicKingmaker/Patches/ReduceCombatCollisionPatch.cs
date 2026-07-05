using HarmonyLib;
using Kingmaker.View;

namespace EddicKingmaker.Patches
{
    /// <summary>
    /// Reduces unit collision size during combat (ported from the CollisionReducer mod).
    /// The game uses a flat 0.5 corpulence out of combat and inflates it to the unit's
    /// real corpulence in combat mode; clamping the getter back to 0.5 removes that
    /// combat inflation, so units can move past each other more easily.
    /// </summary>
    [HarmonyPatch(typeof(UnitMovementAgentBase), nameof(UnitMovementAgentBase.Corpulence), MethodType.Getter)]
    internal static class ReduceCombatCollisionPatch
    {
        private const float MaxCorpulence = 0.5f;

        private static void Postfix(ref float __result)
        {
            if (!Main.Enabled)
                return;
            if (__result > MaxCorpulence)
                __result = MaxCorpulence;
        }
    }
}
