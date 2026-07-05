using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace EddicKingmakerBuffs
{
    public static class Main
    {
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static bool Enabled;

        /// <summary>The mod's folder inside the game's Mods directory.</summary>
        public static string ModFolder;

        // Plain press applies the configured buff routine to the party;
        // with Shift held it dumps the available buffs (and creates a config template) instead.
        private const KeyCode BuffKey = KeyCode.F1;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Logger = modEntry.Logger;
            ModFolder = modEntry.Path;
            modEntry.OnToggle = OnToggle;
            modEntry.OnUpdate = OnUpdate;

            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Logger.Log("EddicKingmakerBuffs loaded.");
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

            if (Input.GetKeyDown(BuffKey))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    Core.BuffDump.Write();
                else
                    Core.BuffExecutor.ExecuteRoutine();
            }
        }
    }
}
