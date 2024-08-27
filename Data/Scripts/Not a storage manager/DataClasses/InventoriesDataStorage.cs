using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.DataClasses
{
    public class InventoriesDataStorage : ModBase, IDisposable
    {
        // ReSharper disable InconsistentNaming

        public Dictionary<string, MyObjectBuilderType> Dictionary_Display_Name_To_ObjectBuilder_TypeId =
            new Dictionary<string, MyObjectBuilderType>(); // Conversions from one entry into Useful thing


        public Dictionary<MyObjectBuilderType, Dictionary<string,MyFixedPoint>> Storage_Dictionary =
            new Dictionary<MyObjectBuilderType, Dictionary<string, MyFixedPoint>>(); // This is global storage of items.


        public List<string> Possible_Display_Name_Entries = new List<string>();

        public InventoriesDataStorage()
        {
            InitializeStorages();
        }

        private void InitializeStorages()
        {
            foreach (var definition in GetDefinitions.Instance.AmmoDefinition)
            {
                var name = definition.DisplayNameText;
                Dictionary_Display_Name_To_ObjectBuilder_TypeId[name] = definition.Id.TypeId;

                FillDictionary(definition);
            }

            foreach (var definition in GetDefinitions.Instance.ComponentsDefinitions)
            {
                var name = definition.DisplayNameText;
                Dictionary_Display_Name_To_ObjectBuilder_TypeId[name] = definition.Id.TypeId;
                FillDictionary(definition);
            }

            // Ore Definitions - Prefixed with "ore_"
            foreach (var definition in GetDefinitions.Instance.OresDefinitions)
            {
                var name = definition.DisplayNameText;
                if (!name.Contains("Ore")) name = "Ore " + name;
                Dictionary_Display_Name_To_ObjectBuilder_TypeId[name] = definition.Id.TypeId;
                FillDictionary(definition);
            }

            // Ingot Definitions - Prefixed with "ingot_"
            foreach (var definition in GetDefinitions.Instance.IngotDefinitions)
            {
                var name = definition.DisplayNameText;
                if (!name.Contains("Ingot")) name = "Ingot " + name;
                Dictionary_Display_Name_To_ObjectBuilder_TypeId[name] = definition.Id.TypeId;
                FillDictionary(definition);

            }
        }

        private void FillDictionary(MyDefinitionBase definition)
        {
            Storage_Dictionary[definition.Id.TypeId] = new Dictionary<string, MyFixedPoint>();
            var access = Storage_Dictionary[definition.Id.TypeId];
            access[definition.Id.SubtypeName] = 0;
        }

        public void Dispose()
        {
            try
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, "OnDispose was called");
                Storage_Dictionary.Clear();
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, $"On dispose error {ex}");
            }
        }
    }
}