using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using Sandbox.ModAPI;
using VRage;
using VRage.Utils;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.DataClasses
{
    public class InventoriesDataStorage:ModBase,IDisposable
    {
        public Dictionary<string, string> TypeIdMyItemStorage = new Dictionary<string, string>();
        public Dictionary<string, MyFixedPoint> SubTypeKeyDictionaryItemStorage = new Dictionary<string, MyFixedPoint>();
        public Dictionary<string, MyFixedPoint> MyItemLimitStorage = new Dictionary<string, MyFixedPoint>();

        public InventoriesDataStorage()
        {
            InitializeStorages();
        }
        private void InitializeStorages()
        {
            foreach (var definition in GetDefinitions.Instance.AmmoDefinition)
            {
                TypeIdMyItemStorage[definition.Id.SubtypeId.String] = definition.DisplayNameText;
                SubTypeKeyDictionaryItemStorage[definition.Id.SubtypeId.String] = 0;
            }

            foreach (var definition in GetDefinitions.Instance.ComponentsDefinitions)
            {
                TypeIdMyItemStorage[definition.Id.SubtypeId.String] = definition.DisplayNameText;
                SubTypeKeyDictionaryItemStorage[definition.Id.SubtypeId.String] = 0;
            }

            foreach (var definition in GetDefinitions.Instance.OresDefinitions)
            {
                TypeIdMyItemStorage[definition.Id.SubtypeId.String] = definition.DisplayNameText;
                SubTypeKeyDictionaryItemStorage[definition.Id.SubtypeId.String] = 0; 
            }

            foreach (var definition in GetDefinitions.Instance.IngotDefinitions)
            {
                TypeIdMyItemStorage[definition.Id.SubtypeId.String] = definition.DisplayNameText;
                SubTypeKeyDictionaryItemStorage[definition.Id.SubtypeId.String] = 0;
            }

            MyItemLimitStorage = new Dictionary<string, MyFixedPoint>(SubTypeKeyDictionaryItemStorage);
        }

        public void Dispose()
        {
            MyAPIGateway.Utilities.ShowMessage(ClassName, "OnDispose was called");
            TypeIdMyItemStorage.Clear();
            SubTypeKeyDictionaryItemStorage.Clear();
            MyItemLimitStorage.Clear();
        }
    }
}