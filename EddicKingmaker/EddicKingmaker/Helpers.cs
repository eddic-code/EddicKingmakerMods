using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Localization;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using UnityEngine;

namespace EddicKingmaker
{
    /// <summary>
    /// Minimal blueprint-creation utilities. Thanks to Krafs.Publicizer we can write
    /// the game's private fields (m_AssetGuid, m_Key, m_DisplayName, ...) directly
    /// instead of going through reflection like the classic mods do.
    /// </summary>
    internal static class Helpers
    {
        // BasicFeatSelection: the selection every character picks general feats from.
        public const string BasicFeatSelectionId = "247a4068296e8be42890143f451b4b45";

        /// <summary>
        /// Loads a PNG from the mod's Icons folder as a sprite (community-standard
        /// pattern, cf. CallOfTheWild's LoadIcons). Returns null on failure so
        /// callers can fall back to a vanilla icon — a missing file must never
        /// break blueprint creation.
        /// </summary>
        public static Sprite LoadSprite(string fileName)
        {
            try
            {
                var path = System.IO.Path.Combine(Main.ModFolder, "Icons", fileName);
                var texture = new Texture2D(2, 2);
                texture.LoadImage(System.IO.File.ReadAllBytes(path));
                return Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
            }
            catch (Exception e)
            {
                Main.Logger.Warning($"Failed to load icon '{fileName}': {e.Message}");
                return null;
            }
        }

        /// <summary>Creates a localized string and registers its text in the current language pack.</summary>
        public static LocalizedString CreateString(string key, string value)
        {
            LocalizationManager.CurrentPack.Strings[key] = value;
            return new LocalizedString { m_Key = key };
        }

        /// <summary>Assigns the blueprint's guid and registers it in the game's blueprint library.</summary>
        public static void AddAsset(BlueprintScriptableObject blueprint, string guid)
        {
            if (string.IsNullOrEmpty(guid))
                throw new ArgumentException($"Missing guid for blueprint '{blueprint.name}'.");
            if (Main.Library.BlueprintsByAssetId.TryGetValue(guid, out var existing))
                throw new InvalidOperationException($"Duplicate guid {guid}: already used by '{existing.name}' ({existing.GetType().Name}).");

            blueprint.m_AssetGuid = guid;
            Main.Library.GetAllBlueprints().Add(blueprint);
            Main.Library.BlueprintsByAssetId[guid] = blueprint;
        }

        public static BlueprintFeature CreateFeature(string name, string displayName, string description,
            string guid, Sprite icon, FeatureGroup group, params BlueprintComponent[] components)
        {
            var feat = ScriptableObject.CreateInstance<BlueprintFeature>();
            feat.name = name;
            feat.m_DisplayName = CreateString(name + ".Name", displayName);
            feat.m_Description = CreateString(name + ".Description", description);
            feat.m_Icon = icon;
            feat.Groups = new[] { group };
            feat.ComponentsArray = components;
            AddAsset(feat, guid);
            return feat;
        }

        /// <summary>Makes the feat pickable by adding it to a feat selection (e.g. BasicFeatSelection).</summary>
        public static void AddToFeatSelection(string selectionId, BlueprintFeature feat)
        {
            var selection = (BlueprintFeatureSelection)Main.Library.BlueprintsByAssetId[selectionId];
            if (!selection.AllFeatures.Contains(feat))
                selection.AllFeatures = selection.AllFeatures.Append(feat).ToArray();
        }

        /// <summary>Blueprint components are ScriptableObjects too; they need a name to serialize cleanly.</summary>
        public static T CreateComponent<T>() where T : BlueprintComponent
        {
            var component = ScriptableObject.CreateInstance<T>();
            component.name = "$" + typeof(T).Name;
            return component;
        }

        public static AddFacts CreateAddFacts(params BlueprintUnitFact[] facts)
        {
            var addFacts = CreateComponent<AddFacts>();
            addFacts.Facts = facts;
            return addFacts;
        }

        public static BlueprintBuff CreateBuff(string name, string displayName, string description,
            string guid, Sprite icon, params BlueprintComponent[] components)
        {
            var buff = ScriptableObject.CreateInstance<BlueprintBuff>();
            buff.name = name;
            buff.m_DisplayName = CreateString(name + ".Name", displayName);
            buff.m_Description = CreateString(name + ".Description", description);
            buff.m_Icon = icon;
            // Null PrefabLinks make the game NRE when the buff is applied; empty links mean "no vfx".
            buff.FxOnStart = new PrefabLink();
            buff.FxOnRemove = new PrefabLink();
            buff.ComponentsArray = components;
            AddAsset(buff, guid);
            return buff;
        }

        /// <summary>
        /// Creates a toggle that applies <paramref name="buff"/> to its owner while active;
        /// the game removes the buff automatically when the toggle is deactivated.
        /// </summary>
        public static BlueprintActivatableAbility CreateToggleAbility(string name, string displayName,
            string description, string guid, Sprite icon, BlueprintBuff buff,
            params BlueprintComponent[] components)
        {
            var ability = ScriptableObject.CreateInstance<BlueprintActivatableAbility>();
            ability.name = name;
            ability.m_DisplayName = CreateString(name + ".Name", displayName);
            ability.m_Description = CreateString(name + ".Description", description);
            ability.m_Icon = icon;
            ability.Buff = buff;
            ability.Group = ActivatableAbilityGroup.None;
            ability.ActivationType = AbilityActivationType.Immediately;
            ability.m_ActivateWithUnitCommand = UnitCommand.CommandType.Free;
            ability.DeactivateImmediately = true;
            ability.ResourceAssetIds = Array.Empty<string>();
            ability.ComponentsArray = components;
            AddAsset(ability, guid);
            return ability;
        }
    }
}
