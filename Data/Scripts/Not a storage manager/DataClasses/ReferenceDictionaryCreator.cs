using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StorageSubclasses;
using Sandbox.Game.Screens.Helpers.RadialMenuActions;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.DataClasses
{
    public class ReferenceDictionaryCreator : ModBase
    {
        // ReSharper disable InconsistentNaming

        public ItemDefinitionStorage ItemDefinitionStorage;

        private readonly ModLogger _modLogger = ModAccessStatic.Instance.Logger;
        public List<string> Possible_Display_Name_Entries = new List<string>();

        public ReferenceDictionaryCreator()
        {
            ItemDefinitionStorage = new ItemDefinitionStorage();
            try
            {
                InitializeStorages();
                _modLogger.Log(ClassName, $"Creating all reference tables");
            }
            catch (Exception ex)
            {
                _modLogger.LogError(ClassName,$"Congrats, this fucked up {ex}");
            }

        }

        private void InitializeStorages()
        {
            if (PreLoadGetDefinitions.Instance == null)
            {
                _modLogger.LogError(ClassName, "GetDefinitions.Instance is null. Initialization aborted.");
                return;
            }
            foreach (var definition in PreLoadGetDefinitions.Instance.AmmoDefinition)
            {
                var name = definition.DisplayNameText;
                FillDictionary(definition, name);
            }

            foreach (var definition in PreLoadGetDefinitions.Instance.ComponentsDefinitions)
            {
                var name = definition.DisplayNameText;
                FillDictionary(definition, name);
            }

            // Ore Definitions - Prefixed with "ore_"
            foreach (var definition in PreLoadGetDefinitions.Instance.OresDefinitions)
            {
                var name = definition.DisplayNameText;
                if(UniqueModExceptions.Contains(name)) continue;
                if (!NamingExceptions.Contains(name))
                {
                    if (!name.Contains("Ore")) name += " Ore";
                }
                FillDictionary(definition, name);
            }

            // Ingot Definitions - Prefixed with "ingot_"
            foreach (var definition in PreLoadGetDefinitions.Instance.IngotDefinitions)
            {
                var name = definition.DisplayNameText;
                FillDictionary(definition, name);
            }
        }

        private void FillDictionary(MyDefinitionBase definition, string name)
        {
            ItemDefinitionStorage.Add(name, definition.Id, 0);
            Possible_Display_Name_Entries.Add(name);
        }

        public override void Dispose()
        {
            try
            {
                _modLogger.LogWarning(ClassName, "OnDispose was called");
                ItemDefinitionStorage = null;
                Possible_Display_Name_Entries.Clear();
            }
            catch (Exception ex)
            {
                _modLogger.LogError(ClassName, $"On dispose error {ex}");
            }
        }
    }
}