using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using Sandbox.Game;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using IMyInventory = VRage.Game.ModAPI.IMyInventory;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.DataClasses
{
    public class MyInventories : ModBase, IDisposable
    {
        public HashSet<IMyInventory> AllInventories = new HashSet<IMyInventory>();

        public Dictionary<IMyInventory, List<MyInventoryItem>> Snapshot =
            new Dictionary<IMyInventory, List<MyInventoryItem>>();

        public InventoriesDataStorage InventoriesData = new InventoriesDataStorage();

        public void AddInventory(IMyInventory inventory)
        {
            try
            {
                AllInventories.Add(inventory);
                if (!inventory.Empty())
                {
                    var items = new List<MyInventoryItem>();
                    inventory.GetItems(items);
                    Snapshot[inventory] = items;
                    foreach (var item in items)
                    {
                        var itemType = item.Type.SubtypeId;
                        var amount = item.Amount;
                        InventoriesData.DictionarySubtypeToMyFixedPoint[itemType] += amount;
                    }
                }

                inventory.OnVolumeChanged += Inventory_OnVolumeChanged;
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, $"On add inventory error {ex}");
            }
        }

        public void RemoveInventory(IMyInventory inventory)
        {
            try
            {
                Snapshot.Remove(inventory);
                AllInventories.Remove(inventory);
                inventory.OnVolumeChanged -= Inventory_OnVolumeChanged;
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, $"On remove inventory error {ex}");
            }
        }

        private void Inventory_OnVolumeChanged(IMyInventory arg1, float arg2, float arg3)
        {
            try
            {
                var newValue = new List<MyInventoryItem>();
                arg1.GetItems(newValue);
                var oldValue = Snapshot[arg1];

                // Group old and new items by SubtypeId and sum their amounts
                var oldGrouped = oldValue
                    .GroupBy(item => item.Type.SubtypeId)
                    .ToDictionary(group => group.Key,
                        group => group.Aggregate(MyFixedPoint.Zero, (total, next) => total + next.Amount));

                var newGrouped = newValue
                    .GroupBy(item => item.Type.SubtypeId)
                    .ToDictionary(group => group.Key,
                        group => group.Aggregate(MyFixedPoint.Zero, (total, next) => total + next.Amount));

                // HashSet to ensure unique SubtypeIds
                var hashSet = new HashSet<string>(oldGrouped.Keys);
                hashSet.UnionWith(newGrouped.Keys);

                foreach (var id in hashSet.Where(id => InventoriesData.DictionarySubtypeToMyFixedPoint.ContainsKey(id)))
                {
                    MyFixedPoint oldValueSum;
                    var oldAmount = oldGrouped.TryGetValue(id, out oldValueSum) ? oldValueSum : MyFixedPoint.Zero;
                    MyFixedPoint newValueSum;
                    var newAmount = newGrouped.TryGetValue(id, out newValueSum) ? newValueSum : MyFixedPoint.Zero;

                    var result = newAmount - oldAmount;

                    // Update the dictionary with the difference
                    InventoriesData.DictionarySubtypeToMyFixedPoint[id] += result;
                }

                // Updating the snapshot.
                Snapshot[arg1] = newValue;
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, $"Inventory on volume changed error {ex}");
            }
        }

        public void Dispose()
        {
            try
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, "OnDispose was called");
                foreach (var inventory in AllInventories)
                {
                    inventory.OnVolumeChanged -= Inventory_OnVolumeChanged;
                }

                AllInventories.Clear();
                Snapshot.Clear();
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, $"On dispose error {ex}");
            }
        }
    }
}