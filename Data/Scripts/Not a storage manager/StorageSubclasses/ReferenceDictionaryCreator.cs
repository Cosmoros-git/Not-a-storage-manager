using System;
using System.Collections.Generic;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.NoIdeaHowToNameFiles;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses;
using VRage.Game;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.StorageSubclasses
{
    public class ReferenceDictionaryCreator : ModBase
    {
        
        private readonly ItemDefinitionStorage _itemDefinitionStorage;

        public List<string> PossibleDisplayNameEntries = new List<string>();

        public ReferenceDictionaryCreator(ItemDefinitionStorage itemDefinitionStorage)
        {
            _itemDefinitionStorage = itemDefinitionStorage;
            try
            {
                InitializeStorages();
                ModLogger.Instance.Log(ClassName, $"Creating all reference tables");
            }
            catch (Exception ex)
            {
                ModLogger.Instance.LogError(ClassName, $"Congrats, this fucked up {ex}");
            }
        }

        private void InitializeStorages()
        {
            ModLogger.Instance.Log(ClassName, "Storage is being filled by data");
            if (PreLoadGetDefinitions.Instance == null)
            {
                ModLogger.Instance.LogError(ClassName, "GetDefinitions.Instance is null. Initialization aborted.");
                return;
            }

            var ammoDef = PreLoadGetDefinitions.Instance.AmmoDefinition;
            // ModLogger.Instance.Log(ClassName, ammoDeff.Count.ToString());
            foreach (var definition in ammoDef)
            {
                var name = definition.DisplayNameText;
                FillDictionary(definition, name);
            }

            var compDef = PreLoadGetDefinitions.Instance.ComponentsDefinitions;
            // ModLogger.Instance.Log(ClassName, compDeff.Count.ToString());
            foreach (var definition in compDef)
            {
                var name = definition.DisplayNameText;
                FillDictionary(definition, name);
            }

            // Ore Definitions - Prefixed with "ore_"
            var oreDef = PreLoadGetDefinitions.Instance.OresDefinitions;
            //  ModLogger.Instance.Log(ClassName, oreDeff.Count.ToString());
            foreach (var definition in oreDef)
            {
                var name = definition.DisplayNameText;
                if (UniqueModExceptions.Contains(name)) continue;
                if (!NamingExceptions.Contains(name))
                {
                    if (!name.Contains("Ore")) name += " Ore";
                }

                FillDictionary(definition, name);
            }

            // Ingot Definitions - Prefixed with "ingot_"
            var ingotDef = PreLoadGetDefinitions.Instance.OresDefinitions;
            // ModLogger.Instance.Log(ClassName,ingotDeff.Count.ToString());
            foreach (var definition in ingotDef)
            {
                var name = definition.DisplayNameText;
                FillDictionary(definition, name);
            }

            ModLogger.Instance.Log(ClassName, "Possible name entries: "+PossibleDisplayNameEntries.Count);
        }

        private void FillDictionary(MyDefinitionBase definition, string name)
        {
            _itemDefinitionStorage.Add(name, definition.Id, 0);
            PossibleDisplayNameEntries.Add(name);
            ModLogger.Instance.Log(ClassName, $"Possible name added: {name}");
        }
    }
}