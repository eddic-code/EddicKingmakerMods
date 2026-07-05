using HarmonyLib;
using Kingmaker.UnitLogic;

namespace EddicKingmaker.Patches
{
    /// <summary>
    /// Multiplies carrying capacity by 10 (pattern from BagOfTricks). The light and
    /// medium encumbrance thresholds are derived from GetHeavy (1/3 and 2/3 of it),
    /// and party capacity is the sum of the members' values, so scaling this single
    /// method scales the whole encumbrance system consistently.
    /// </summary>
    [HarmonyPatch(typeof(EncumbranceHelper), nameof(EncumbranceHelper.GetHeavy))]
    internal static class CarryWeightPatch
    {
        private const int CarryWeightMultiplier = 10;

        private static void Postfix(ref int __result)
        {
            if (!Main.Enabled)
                return;
            __result *= CarryWeightMultiplier;
        }
    }
}
