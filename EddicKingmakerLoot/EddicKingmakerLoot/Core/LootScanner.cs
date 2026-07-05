using System;
using System.Collections.Generic;
using Kingmaker;
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

            var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            ScanUnits(names);
            ScanMapObjects(names);

            string areaName = Game.Instance.CurrentlyLoadedArea.AreaDisplayName;
            if (names.Count == 0)
            {
                GameLog.Message($"No lootable items in {areaName}.");
                return;
            }

            GameLog.Message($"Lootable items in {areaName} ({names.Count}):");
            foreach (string name in names)
                GameLog.Message("  " + name);
        }

        /// <summary>Loot carried by units: enemies-to-be-killed and dead bodies alike.</summary>
        private static void ScanUnits(ISet<string> names)
        {
            foreach (var unit in Game.Instance.State.Units)
            {
                if (unit.IsPlayerFaction || unit.Descriptor.State.HasCondition(UnitCondition.Unlootable))
                    continue;

                AddItems(names, unit.Inventory);
            }
        }

        /// <summary>Loot in map containers: chests, environment stashes, dropped bags.</summary>
        private static void ScanMapObjects(ISet<string> names)
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
                        AddItems(names, data.Loot);
                }
            }
        }

        private static void AddItems(ISet<string> names, IEnumerable<ItemEntity> items)
        {
            foreach (var item in items)
            {
                // ItemEntity.Name shows NonIdentifiedName for unidentified items,
                // matching what the player sees in loot windows.
                if (item.IsLootable && !string.IsNullOrEmpty(item.Name))
                    names.Add(item.Name);
            }
        }
    }
}
