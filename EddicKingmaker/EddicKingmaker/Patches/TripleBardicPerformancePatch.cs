using HarmonyLib;
using Kingmaker.Blueprints;

namespace EddicKingmaker.Patches
{
    /// <summary>
    /// Triples the bardic performance pool. A blueprint edit can't do this: the
    /// pool total is only assembled at runtime in GetMaxAmount — the resource's
    /// own per-level part, then every IResourceAmountBonusHandler bonus over the
    /// event bus (IncreaseResourcesByClass: bard level + Cha + flat 2, Extra
    /// Performance feats, items). Postfixing the final result multiplies all of
    /// it: vanilla 2×level + Cha + 2 becomes 3×(2×level + Cha + 2), and feat or
    /// item charges are tripled alongside. Note the pool is shared: Sensei monk
    /// toggles and a few items drain the same resource and benefit equally.
    /// </summary>
    [HarmonyPatch(typeof(BlueprintAbilityResource), nameof(BlueprintAbilityResource.GetMaxAmount))]
    internal static class TripleBardicPerformancePatch
    {
        // BardicPerformanceResource (guid from blueprints.txt).
        private const string BardicPerformanceResourceId = "e190ba276831b5c4fa28737e5e49e6a6";

        private const int Multiplier = 3;

        private static void Postfix(BlueprintAbilityResource __instance, ref int __result)
        {
            if (!Main.Enabled)
                return;

            if (__instance.AssetGuid == BardicPerformanceResourceId)
                __result *= Multiplier;
        }
    }
}
