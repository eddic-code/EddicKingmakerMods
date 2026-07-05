using System;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Items;
using Kingmaker.UnitLogic;
using Kingmaker.View.MapObjects;

namespace EddicKingmakerLoot.Core
{
    /// <summary>
    /// Scans the current area for lootable items (non-party unit inventories and
    /// loot containers) and prints their names to the in-game log window.
    /// </summary>
    public static class LootScanner
    {
        public static void ListAreaLoot()
        {
            if (Game.Instance?.CurrentlyLoadedArea == null)
                return;

            var itemsByName = new SortedDictionary<string, ItemEntity>(StringComparer.OrdinalIgnoreCase);
            ScanUnits(itemsByName);
            ScanMapObjects(itemsByName);

            string areaName = Game.Instance.CurrentlyLoadedArea.AreaDisplayName;
            string filterNote = Main.Settings.HideVendorTrash ? ", vendor trash hidden" : "";
            if (itemsByName.Count == 0)
            {
                GameLog.Message($"No lootable items in {areaName}{filterNote}.");
                return;
            }

            GameLog.Message($"Lootable items in {areaName} ({itemsByName.Count}{filterNote}):");
            foreach (var entry in itemsByName)
            {
                // A detached copy as tooltip shows the hoverable item card (like vanilla
                // loot lines) without the log holding a reference to the live item.
                GameLog.Message("  " + entry.Key,
                    tooltip: ItemsEntityFactory.CreateItemCopy(entry.Value, 1));
            }
        }

        /// <summary>Loot carried by units: enemies-to-be-killed and dead bodies alike.</summary>
        private static void ScanUnits(IDictionary<string, ItemEntity> itemsByName)
        {
            foreach (var unit in Game.Instance.State.Units)
            {
                if (unit.IsPlayerFaction || unit.Descriptor.State.HasCondition(UnitCondition.Unlootable))
                    continue;

                AddItems(itemsByName, unit.Inventory);
            }
        }

        /// <summary>Loot in map containers: chests, environment stashes, dropped bags.</summary>
        private static void ScanMapObjects(IDictionary<string, ItemEntity> itemsByName)
        {
            foreach (var mapObject in Game.Instance.State.MapObjects)
            {
                var interactions = mapObject.View?.Interactions;
                if (interactions == null)
                    continue;

                foreach (var interaction in interactions)
                {
                    // PlayerChest is the shared stash — the player's own storage, not area loot.
                    if (!(interaction is LootComponent loot) || loot.LootContainerType == LootContainerType.PlayerChest)
                        continue;

                    var data = mapObject.GetComponentData<LootComponent.LootPersistentData>();
                    if (data?.Loot != null)
                        AddItems(itemsByName, data.Loot);
                }
            }
        }

        private static void AddItems(IDictionary<string, ItemEntity> itemsByName, IEnumerable<ItemEntity> items)
        {
            foreach (var item in items)
            {
                // MiscellaneousType != None (gems, jewellery, animal parts) is the game's
                // own sell-junk classification, used by the vendor mass-sale option.
                // MoneyReplacement marks money items (Gold Coins) that convert to gold on pickup.
                if (Main.Settings.HideVendorTrash
                    && (item.Blueprint.MiscellaneousType != BlueprintItem.MiscellaneousItemType.None
                        || item.Blueprint.GetComponent<MoneyReplacement>() != null))
                    continue;

                // ItemEntity.Name shows NonIdentifiedName for unidentified items,
                // matching what the player sees in loot windows.
                if (item.IsLootable && !string.IsNullOrEmpty(item.Name) && !itemsByName.ContainsKey(item.Name))
                    itemsByName.Add(item.Name, item);
            }
        }
    }
}
