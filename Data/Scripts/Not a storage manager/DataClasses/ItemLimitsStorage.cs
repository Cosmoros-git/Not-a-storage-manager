using System.Collections.Generic;
using System.Linq;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StorageSubclasses;
using Sandbox.ModAPI;
using VRage.Game;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.DataClasses
{
    public class ItemLimitsStorage : ModBase
    {
        public readonly Dictionary<MyDefinitionId, int> DictionaryTrackedValues = new Dictionary<MyDefinitionId, int>();

        public readonly Dictionary<IMyConveyorSorter, Dictionary<MyDefinitionId, ModTuple>> MyItemLimitsCounts =
            new Dictionary<IMyConveyorSorter, Dictionary<MyDefinitionId, ModTuple>>();

        public ItemLimitsStorage()
        {
            HeartBeat100 += RemoveZeroTrackedValues;
        }

        public void RemoveZeroTrackedValues()
        {
            var keysToRemove = DictionaryTrackedValues.Where(kv => kv.Value == 0).Select(kv => kv.Key).ToList();
            foreach (var key in keysToRemove)
            {
                DictionaryTrackedValues.Remove(key);
            }
        }

        public override void Dispose()
        {
            HeartBeat100 -= RemoveZeroTrackedValues;
        }
    }
}