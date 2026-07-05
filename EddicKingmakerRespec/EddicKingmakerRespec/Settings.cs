using UnityEngine;
using UnityModManagerNet;

namespace EddicKingmakerRespec
{
    /// <summary>Persisted in Settings.xml inside the mod folder by UMM.</summary>
    public class Settings : UnityModManager.ModSettings
    {
        public KeyBinding RespecKey = new KeyBinding { keyCode = KeyCode.F2 };

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
