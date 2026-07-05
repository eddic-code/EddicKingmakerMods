using System;
using System.Reflection;
using HarmonyLib;
using Kingmaker.Blueprints;
using UnityEngine;
using UnityModManagerNet;

namespace EddicKingmaker
{
    public static class Main
    {
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static bool Enabled;

        /// <summary>The game's blueprint library; set once it has finished loading.</summary>
        public static LibraryScriptableObject Library;

        /// <summary>The mod's folder inside the game's Mods directory.</summary>
        public static string ModFolder;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Logger = modEntry.Logger;
            ModFolder = modEntry.Path;
            modEntry.OnToggle = OnToggle;
            modEntry.OnUpdate = OnUpdate;

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Logger.Log("EddicKingmaker loaded.");
            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            return true;
        }

        // Plain press toggles the partial highlight (lootables/interactables only);
        // with Shift held it dismisses lingering area effect spells instead.
        private const KeyCode HighlightAndDismissKey = KeyCode.Q;

        // Dev utility: dumps all blueprint guids to Mods\EddicKingmaker\blueprints.txt.
        private const KeyCode BlueprintDumpKey = KeyCode.F12;

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float delta)
        {
            if (!Enabled)
                return;

            if (Input.GetKeyDown(HighlightAndDismissKey))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    AreaEffectDismissal.DismissWhitelisted();
                else
                    Patches.PartialHighlightManager.Toggle();
            }

            if (Input.GetKeyDown(BlueprintDumpKey))
            {
                BlueprintDump.DumpAll();
            }
        }

        /// <summary>
        /// Entry point for all blueprint creation. The game populates its blueprint
        /// library in LoadDictionary; once that finishes we can add our own content.
        /// </summary>
        [HarmonyPatch(typeof(LibraryScriptableObject), nameof(LibraryScriptableObject.LoadDictionary))]
        private static class LibraryScriptableObject_LoadDictionary_Patch
        {
            private static void Postfix(LibraryScriptableObject __instance)
            {
                if (Library != null) return; // LoadDictionary runs more than once; only init once.
                Library = __instance;

                try
                {
                    Feats.AegisFeat.Create();
                    Feats.BastionFeat.Create();
                    Feats.SevenfoldVeilFeat.Create();
                    Tweaks.SwordSaintClassSkills.Apply();
                    Tweaks.SpellDurationTweaks.Apply();
                    Tweaks.CraneStylePrerequisites.Apply();
                    Tweaks.NoSkillCheckDCAdjustment.Apply();
                    Logger.Log("Blueprints initialized.");
                }
                catch (Exception e)
                {
                    Logger.LogException(e);
                }
            }
        }
    }
}
