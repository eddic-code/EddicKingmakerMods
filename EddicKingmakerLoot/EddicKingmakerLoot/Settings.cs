using UnityEngine;
using UnityModManagerNet;

namespace EddicKingmakerLoot
{
    /// <summary>Persisted in Settings.xml inside the mod folder by UMM.</summary>
    public class Settings : UnityModManager.ModSettings
    {
        public KeyBinding ListAreaLootKey = new KeyBinding { keyCode = KeyCode.F3 };

        /// <summary>Hide sell-junk from the loot list: items with a MiscellaneousType
        /// (gems, jewellery, animal parts) — the same set as the vendor screen's
        /// mass-sale "gems/animal parts" option.</summary>
        public bool HideVendorTrash = true;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
