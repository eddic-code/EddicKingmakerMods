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
        /// <summary>Prints all lootable items in the current area to the in-game log.</summary>
        private const KeyCode ListAreaLootKey = KeyCode.F3;

        public static UnityModManager.ModEntry.ModLogger Logger;
        public static bool Enabled;

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

            if (Input.GetKeyDown(ListAreaLootKey))
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
    }
}
