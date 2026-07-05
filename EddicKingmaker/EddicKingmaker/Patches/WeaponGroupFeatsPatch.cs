using System;
using System.Collections.Generic;
using HarmonyLib;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.UI.Common;

namespace EddicKingmaker.Patches
{
    /// <summary>
    /// Weapon-choice feats (Weapon Focus, Improved Critical, Weapon Specialization,
    /// ...) apply to a whole weapon group instead of a single weapon (ported from
    /// the WeaponFocusPlus mod; its groups.json is hardcoded below). The game
    /// stores the chosen weapon as a FeatureParam and asks
    /// "param == this weapon's category?" whenever such a feat is checked, so
    /// making same-group categories compare equal upgrades every weapon-choice
    /// feat (and their prerequisites) at once.
    /// </summary>
    internal static class WeaponGroupFeats
    {
        private static readonly Dictionary<WeaponCategory, string> GroupByCategory = BuildGroupMap();

        private static Dictionary<WeaponCategory, string> BuildGroupMap()
        {
            // Weapon groups from WeaponFocusPlus' groups.json; only the flattened
            // category->group map is kept alive after initialization.
            var groups = new Dictionary<string, WeaponCategory[]>
            {
                ["Axes"] = new[]
                {
                    WeaponCategory.Battleaxe, WeaponCategory.DwarvenWaraxe, WeaponCategory.Greataxe,
                    WeaponCategory.Handaxe, WeaponCategory.HeavyPick, WeaponCategory.LightPick,
                    WeaponCategory.Tongi,
                },
                ["Heavy Blades"] = new[]
                {
                    WeaponCategory.BastardSword, WeaponCategory.DuelingSword, WeaponCategory.ElvenCurvedBlade,
                    WeaponCategory.Estoc, WeaponCategory.Falcata, WeaponCategory.Falchion,
                    WeaponCategory.Greatsword, WeaponCategory.Longsword, WeaponCategory.Scimitar,
                    WeaponCategory.Scythe,
                },
                ["Light Blades"] = new[]
                {
                    WeaponCategory.Dagger, WeaponCategory.Kukri, WeaponCategory.Rapier,
                    WeaponCategory.Shortsword, WeaponCategory.Sickle, WeaponCategory.Starknife,
                },
                ["Bows"] = new[]
                {
                    WeaponCategory.Longbow, WeaponCategory.Shortbow,
                },
                ["Crossbows"] = new[]
                {
                    WeaponCategory.HandCrossbow, WeaponCategory.HeavyCrossbow, WeaponCategory.HeavyRepeatingCrossbow,
                    WeaponCategory.LightCrossbow, WeaponCategory.LightRepeatingCrossbow,
                },
                ["Double"] = new[]
                {
                    WeaponCategory.DoubleAxe, WeaponCategory.DoubleSword, WeaponCategory.HookedHammer,
                    WeaponCategory.Urgrosh,
                },
                ["Flails & Hammers"] = new[]
                {
                    WeaponCategory.Club, WeaponCategory.EarthBreaker, WeaponCategory.Flail,
                    WeaponCategory.Greatclub, WeaponCategory.HeavyFlail, WeaponCategory.HeavyMace,
                    WeaponCategory.LightHammer, WeaponCategory.LightMace, WeaponCategory.Warhammer,
                },
                ["Polearms"] = new[]
                {
                    WeaponCategory.Bardiche, WeaponCategory.Fauchard, WeaponCategory.Glaive,
                },
                ["Spears"] = new[]
                {
                    WeaponCategory.Longspear, WeaponCategory.Shortspear, WeaponCategory.Spear,
                    WeaponCategory.Trident,
                },
                ["Thrown"] = new[]
                {
                    WeaponCategory.Bomb, WeaponCategory.Dart, WeaponCategory.Javelin,
                    WeaponCategory.Sling, WeaponCategory.SlingStaff, WeaponCategory.ThrowingAxe,
                },
                ["Close"] = new[]
                {
                    WeaponCategory.PunchingDagger, WeaponCategory.SpikedLightShield, WeaponCategory.SpikedHeavyShield,
                    WeaponCategory.WeaponHeavyShield, WeaponCategory.WeaponLightShield,
                },
                ["Monk"] = new[]
                {
                    WeaponCategory.Kama, WeaponCategory.Nunchaku, WeaponCategory.Quarterstaff,
                    WeaponCategory.Sai, WeaponCategory.Shuriken, WeaponCategory.Siangham,
                },
                ["Natural"] = new[]
                {
                    WeaponCategory.UnarmedStrike, WeaponCategory.Bite, WeaponCategory.Claw,
                    WeaponCategory.Gore, WeaponCategory.OtherNaturalWeapons,
                },
            };

            var map = new Dictionary<WeaponCategory, string>();
            foreach (var group in groups)
            {
                foreach (var category in group.Value)
                    map[category] = group.Key;
            }
            return map;
        }

        private static string GetGroup(WeaponCategory category)
        {
            return GroupByCategory.TryGetValue(category, out var group) ? group : null;
        }

        /// <summary>
        /// Core patch: same-group weapon categories compare equal. Everything else
        /// about the params (blueprint, school, stat) must still match, and vanilla
        /// equality is never taken away — only extended.
        /// </summary>
        [HarmonyPatch(typeof(FeatureParam), nameof(FeatureParam.Equals), typeof(FeatureParam))]
        private static class FeatureParam_Equals_Patch
        {
            private static void Postfix(FeatureParam __instance, FeatureParam other, ref bool __result)
            {
                // NB: `is null`, not `== null` — FeatureParam's == operator calls
                // Equals, which would re-enter this postfix.
                if (!Main.Enabled || __result || other is null)
                    return;
                if (__instance.WeaponCategory == null || other.WeaponCategory == null)
                    return;
                if (!object.Equals(__instance.Blueprint, other.Blueprint)
                    || !Nullable.Equals(__instance.SpellSchool, other.SpellSchool)
                    || !Nullable.Equals(__instance.StatType, other.StatType))
                    return;

                var group = GetGroup(__instance.WeaponCategory.Value);
                if (group != null && group == GetGroup(other.WeaponCategory.Value))
                    __result = true;
            }
        }

        /// <summary>Level-up UI: weapon choices read "Longsword - Heavy Blades".</summary>
        [HarmonyPatch(typeof(StatsStrings), nameof(StatsStrings.GetText), typeof(WeaponCategory))]
        private static class StatsStrings_GetText_Patch
        {
            private static void Postfix(WeaponCategory stat, ref string __result)
            {
                if (!Main.Enabled || __result == null)
                    return;
                var group = GetGroup(stat);
                if (group != null)
                    __result = $"{__result} - {group}";
            }
        }

        /// <summary>Inventory tooltip: shows which group a weapon belongs to.</summary>
        [HarmonyPatch(typeof(UIUtilityItem), nameof(UIUtilityItem.GetHandUse))]
        private static class UIUtilityItem_GetHandUse_Patch
        {
            [HarmonyPriority(0)]
            private static void Postfix(ItemEntity item, ref string __result)
            {
                if (!Main.Enabled || __result == null || !(item is ItemEntityWeapon weapon))
                    return;
                var group = GetGroup(weapon.Blueprint.Category);
                if (group != null)
                    __result = $"{__result} ({group})";
            }
        }
    }
}
