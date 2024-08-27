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
    public class InventoriesDataStorage : ModBase, IDisposable
    {
        public Dictionary<string, string> DictionarySubtypeToDisplayName = new Dictionary<string, string>();
        public Dictionary<string, string> DictionaryDisplayNameToSubtype = new Dictionary<string, string>(); // Conversions from one side to other.

        public Dictionary<string, MyFixedPoint> DictionarySubtypeToMyFixedPoint = new Dictionary<string, MyFixedPoint>(); // This is global storage of items.

        public InventoriesDataStorage()
        {
            InitializeStorages();
        }

        private void InitializeStorages()
        {
            foreach (var definition in GetDefinitions.Instance.AmmoDefinition)
            {
                DictionarySubtypeToDisplayName[definition.Id.SubtypeId.String] = definition.DisplayNameText;
                DictionaryDisplayNameToSubtype[definition.DisplayNameText] = definition.DisplayNameText;
                DictionarySubtypeToMyFixedPoint[definition.Id.SubtypeId.String] = 0;
            }

            foreach (var definition in GetDefinitions.Instance.ComponentsDefinitions)
            {
                DictionarySubtypeToDisplayName[definition.Id.SubtypeId.String] = definition.DisplayNameText;
                DictionaryDisplayNameToSubtype[definition.DisplayNameText] = definition.DisplayNameText;
                DictionarySubtypeToMyFixedPoint[definition.Id.SubtypeId.String] = 0;
            }

            foreach (var definition in GetDefinitions.Instance.OresDefinitions)
            {
                DictionarySubtypeToDisplayName[definition.Id.SubtypeId.String] = definition.DisplayNameText;
                DictionaryDisplayNameToSubtype[definition.DisplayNameText] = definition.DisplayNameText;
                DictionarySubtypeToMyFixedPoint[definition.Id.SubtypeId.String] = 0;
            }

            foreach (var definition in GetDefinitions.Instance.IngotDefinitions)
            {   
                DictionarySubtypeToDisplayName[definition.Id.SubtypeId.String] = definition.DisplayNameText;
                DictionaryDisplayNameToSubtype[definition.DisplayNameText] = definition.DisplayNameText;
                DictionarySubtypeToMyFixedPoint[definition.Id.SubtypeId.String] = 0;
            }
        }

        public void Dispose()
        {
            try
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, "OnDispose was called");
                DictionarySubtypeToDisplayName.Clear();
                DictionarySubtypeToMyFixedPoint.Clear();
                DictionarySubtypeToDisplayName.Clear();
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, $"On dispose error {ex}");
            }
        }
    }
}