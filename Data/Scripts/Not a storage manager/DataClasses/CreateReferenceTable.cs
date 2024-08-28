﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StorageSubclasses;
using Sandbox.Game.Screens.Helpers.RadialMenuActions;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.DataClasses
{
    public class CreateReferenceTable : ModBase
    {
        // ReSharper disable InconsistentNaming

        public ItemStorage ItemStorage;


        public List<string> Possible_Display_Name_Entries = new List<string>();

        public CreateReferenceTable()
        {
            ItemStorage = new ItemStorage();
            try
            {
                InitializeStorages();
                MyAPIGateway.Utilities.ShowMessage(ClassName, $"Creating all reference tables");
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName,$"Congrats, this fucked up {ex}");
            }

        }

        private void InitializeStorages()
        {
            if (GetDefinitions.Instance == null)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, "GetDefinitions.Instance is null. Initialization aborted.");
                return;
            }
            foreach (var definition in GetDefinitions.Instance.AmmoDefinition)
            {
                var name = definition.DisplayNameText;
                FillDictionary(definition, name);
            }

            foreach (var definition in GetDefinitions.Instance.ComponentsDefinitions)
            {
                var name = definition.DisplayNameText;
                FillDictionary(definition, name);
            }

            // Ore Definitions - Prefixed with "ore_"
            foreach (var definition in GetDefinitions.Instance.OresDefinitions)
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
            foreach (var definition in GetDefinitions.Instance.IngotDefinitions)
            {
                var name = definition.DisplayNameText;
                FillDictionary(definition, name);
            }
        }

        private void FillDictionary(MyDefinitionBase definition, string name)
        {
            ItemStorage.Add(name, definition.Id, 0);
            Possible_Display_Name_Entries.Add(name);
        }

        public override void Dispose()
        {
            try
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, "OnDispose was called");
                ItemStorage = null;
                Possible_Display_Name_Entries.Clear();
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, $"On dispose error {ex}");
            }
        }
    }
}