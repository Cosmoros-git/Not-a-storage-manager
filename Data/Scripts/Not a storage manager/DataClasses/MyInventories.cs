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

        public Dictionary<IMyInventory, List<MyInventoryItem>> Snapshot = new Dictionary<IMyInventory, List<MyInventoryItem>>();

        public InventoriesDataStorage InventoriesData = new InventoriesDataStorage();
        
        public void AddInventory(IMyInventory inventory)
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
                    InventoriesData.SubTypeKeyDictionaryItemStorage[itemType] += amount;
                }
            }

            inventory.OnVolumeChanged += Inventory_OnVolumeChanged;
        }

        public void RemoveInventory(IMyInventory inventory)
        {
            Snapshot.Remove(inventory);
            AllInventories.Remove(inventory);
            inventory.OnVolumeChanged-= Inventory_OnVolumeChanged;
        }

        private void Inventory_OnVolumeChanged(IMyInventory arg1, float arg2, float arg3)
        {
            var hashSet = new HashSet<string>();
            var newValue = new List<MyInventoryItem>();
            var oldValue = Snapshot[arg1];
            arg1.GetItems(newValue);
            // This gets items out of inventories. The old snapshot and new inventory data.

            // This adds Id's into HashSet making sure Ids are unique and not repeated.
            foreach (var item in newValue)
            {
                hashSet.Add(item.Type.SubtypeId);
            }

            foreach (var item in oldValue)
            {
                hashSet.Add(item.Type.SubtypeId);
            }
            

            // This just deals with math of previous and new values.
            foreach (var id in hashSet)
            {
                if (!InventoriesData.SubTypeKeyDictionaryItemStorage.ContainsKey(id)) return;
                var oldAmount = oldValue.Where(value => value.Type.SubtypeId == id)
                    .Aggregate<MyInventoryItem, MyFixedPoint>(0, (current, value) => current + value.Amount);
                var newAmount = newValue.Where(value => value.Type.SubtypeId == id)
                    .Aggregate<MyInventoryItem, MyFixedPoint>(0, (current, value) => current + value.Amount);
                var result = newAmount - oldAmount;
                InventoriesData.SubTypeKeyDictionaryItemStorage[id] += result;
            }
            // Updating the snapshot.
            Snapshot[arg1] = newValue;
        }

        public void Dispose()
        {
            MyAPIGateway.Utilities.ShowMessage(ClassName, "OnDispose was called");
            foreach (var inventory in AllInventories)
            {
                inventory.OnVolumeChanged -= Inventory_OnVolumeChanged;
            }
            AllInventories.Clear();
            Snapshot.Clear();
        }
    }
}