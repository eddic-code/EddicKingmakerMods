using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace EddicKingmakerRespec
{
    public static class Main
    {
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static bool Enabled;
        public static Settings Settings;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Logger = modEntry.Logger;
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            modEntry.OnToggle = OnToggle;
            modEntry.OnUpdate = OnUpdate;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Logger.Log("EddicKingmakerRespec loaded.");
            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            return true;
        }

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float delta)
        {
            if (!Enabled)
                return;

            // Opens the game's own respec character selector (the Enhanced Edition
            // window), free of charge and without advancing game time.
            if (Settings.RespecKey.Down())
                Core.RespecService.OpenCharacterSelector();
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            UnityModManager.UI.DrawKeybindingSmart(Settings.RespecKey, "Open respec selector", null, GUILayout.ExpandWidth(false));
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }
    }
}
