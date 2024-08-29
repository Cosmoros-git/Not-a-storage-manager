using NotAStorageManager.Data.Scripts.Not_a_storage_manager.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.GridAndBlockManagers;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.NoIdeaHowToNameFiles;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StorageSubclasses;
using Sandbox.Definitions;
using VRage.Game.ModAPI;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses
{
    public class ModAccessStatic
    {
        public static ModAccessStatic Instance { get; private set; }

        public ReferenceDictionaryCreator ReferenceTable { get; private set; }
        public ItemLimitsStorage ItemLimitsStorage { get; private set; }
        public ItemDefinitionStorage ItemDefinitionStorage { get; private set; }
        public InventoryScanner InventoryScanner { get; private set; }
        public TrashSorterStorage TrashSorterStorage { get; private set; }
        public InventoryTerminalManager InventoryTerminalManager { get; private set; }

        public ModAccessStatic()
        {
            Instance = this;
        }

        public void InitiateValues(ReferenceDictionaryCreator referenceTable,
            ItemLimitsStorage itemLimitsStorage,
            ItemDefinitionStorage itemDefinitionStorage,
            InventoryScanner inventoryScanner,
            TrashSorterStorage trashSorterStorage,
            InventoryTerminalManager inventoryTerminalManager)
        {
            ReferenceTable = referenceTable;
            ItemLimitsStorage = itemLimitsStorage;
            ItemDefinitionStorage = itemDefinitionStorage;
            InventoryScanner = inventoryScanner;
            TrashSorterStorage = trashSorterStorage;
            InventoryTerminalManager = inventoryTerminalManager;
        }
    }
}