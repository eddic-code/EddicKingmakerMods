using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;

namespace EddicKingmakerLoot
{
    public static class Main
    {
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static bool Enabled;

        /// <summary>The mod's folder inside the game's Mods directory.</summary>
        public static string ModFolder;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Logger = modEntry.Logger;
            ModFolder = modEntry.Path;
            modEntry.OnToggle = OnToggle;

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
    }
}
