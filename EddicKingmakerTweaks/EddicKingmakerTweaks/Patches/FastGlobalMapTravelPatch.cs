using HarmonyLib;
using Kingmaker.Controllers.GlobalMap;

namespace EddicKingmakerTweaks.Patches
{
    /// <summary>
    /// Global map travel is 1.5x faster (pattern from BagOfTricks). The regional
    /// modifier multiplies into MapMovementController.CalcSpeedModifiers, which
    /// drives both the marker's visual speed and the in-game time consumed per
    /// distance (UpdateGameTime: hours = distance / speed), so travel is faster
    /// on screen and costs proportionally less game time. Both overloads are
    /// patched: the parameterless one drives actual travel, the Vector3 one is
    /// used by GlobalMapEdge for travel-time estimates.
    /// </summary>
    internal static class FastGlobalMapTravelPatch
    {
        private const float TravelSpeedMultiplier = 1.5f;

        [HarmonyPatch(typeof(MapMovementController), nameof(MapMovementController.GetRegionalModifier), new System.Type[0])]
        private static class CurrentRegionPatch
        {
            private static void Postfix(ref float __result)
            {
                if (Main.Enabled)
                    __result *= TravelSpeedMultiplier;
            }
        }

        [HarmonyPatch(typeof(MapMovementController), nameof(MapMovementController.GetRegionalModifier), typeof(UnityEngine.Vector3))]
        private static class PositionPatch
        {
            private static void Postfix(ref float __result)
            {
                if (Main.Enabled)
                    __result *= TravelSpeedMultiplier;
            }
        }
    }
}
