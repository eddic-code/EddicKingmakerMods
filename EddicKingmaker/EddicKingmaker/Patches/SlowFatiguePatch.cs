using HarmonyLib;
using Kingmaker.UnitLogic.Parts;

namespace EddicKingmaker.Patches
{
    /// <summary>
    /// Makes fatigue build up half as fast (pattern from BagOfTricks). Characters gain
    /// a weariness stack every (16 + CON) * GetFatigueHoursModifier() in-game hours,
    /// so doubling the modifier doubles the time between stacks.
    /// </summary>
    [HarmonyPatch(typeof(UnitPartWeariness), nameof(UnitPartWeariness.GetFatigueHoursModifier))]
    internal static class SlowFatiguePatch
    {
        private const float FatigueHoursMultiplier = 2f;

        private static void Postfix(ref float __result)
        {
            if (!Main.Enabled)
                return;
            __result *= FatigueHoursMultiplier;
        }
    }
}
