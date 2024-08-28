using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using VRage.Game;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.StorageSubclasses
{
    public class ItemLimitsStorage
    {
        public readonly Dictionary<MyDefinitionId, int> DictionaryTrackedValues;
        public readonly Dictionary<IMyConveyorSorter, Dictionary<MyDefinitionId, ModTuple>> MyItemLimitsCounts;

    }
}
