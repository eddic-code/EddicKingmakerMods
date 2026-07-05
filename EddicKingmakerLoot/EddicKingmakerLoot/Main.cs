using System;
using System.Reflection;
using EddicKingmakerLoot.Core;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace EddicKingmakerLoot
{
    public static class Main
    {
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static bool Enabled;
        public static Settings Settings;

        /// <summary>The mod's folder inside the game's Mods directory.</summary>
        public static string ModFolder;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Logger = modEntry.Logger;
            ModFolder = modEntry.Path;
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            modEntry.OnToggle = OnToggle;
            modEntry.OnUpdate = OnUpdate;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Logger.Log("EddicKingmakerLoot loaded.");
            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            return true;
        }

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float dt)
        {
            if (!Enabled)
                return;

            if (Settings.ListAreaLootKey.Down())
            {
                try
                {
                    LootScanner.ListAreaLoot();
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            UnityModManager.UI.DrawKeybindingSmart(Settings.ListAreaLootKey, "List area loot", null, GUILayout.ExpandWidth(false));
            Settings.HideVendorTrash = GUILayout.Toggle(Settings.HideVendorTrash,
                " Hide vendor trash (gems, jewellery, animal parts)", GUILayout.ExpandWidth(false));
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }
    }
}
