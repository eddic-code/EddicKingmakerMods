using HarmonyLib;
using Kingmaker.Kingdom.Tasks;

namespace EddicKingmaker.Patches
{
    /// <summary>
    /// Kingdom missions resolve in a single day (functionality ported from the
    /// KingdomResolution mod, reduced to a fixed one-day clamp).
    /// </summary>
    internal static class KingdomOneDayResolutionPatch
    {
        /// <summary>
        /// Days until an assigned event/project completes (vanilla default: 14).
        /// Events the baron resolves personally in the throne room are excluded,
        /// mirroring the reference mod.
        /// </summary>
        [HarmonyPatch(typeof(KingdomEvent), nameof(KingdomEvent.CalculateResolutionTime))]
        private static class ResolutionTimePatch
        {
            private static void Postfix(KingdomEvent __instance, ref int __result)
            {
                if (!Main.Enabled || __instance.EventBlueprint.IsResolveByBaron)
                    return;
                if (__result > 1)
                    __result = 1;
            }
        }

        /// <summary>
        /// Days of the ruler's personal time a project consumes (the game
        /// auto-advances the calendar by this when the project starts).
        /// KingdomTaskEvent.SkipPlayerTime, the UI preview and the kingdom AI all
        /// read this method, so clamping here keeps them consistent.
        /// </summary>
        [HarmonyPatch(typeof(KingdomEvent), nameof(KingdomEvent.CalculateRulerTime))]
        private static class RulerTimePatch
        {
            private static void Postfix(ref int __result)
            {
                if (!Main.Enabled)
                    return;
                if (__result > 1)
                    __result = 1;
            }
        }
    }
}
