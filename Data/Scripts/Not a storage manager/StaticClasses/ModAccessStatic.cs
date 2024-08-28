using NotAStorageManager.Data.Scripts.Not_a_storage_manager.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StorageSubclasses;
using Sandbox.Definitions;
using VRage.Game.ModAPI;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses
{
    public class ModAccessStatic
    {
        public static ModAccessStatic Instance { get; set; }

        public ModAccessStatic()
        {
            Instance = this;
            InventoryScanner = new InventoryScanner();
            ItemStorage = InventoryScanner.ReferenceData.ItemStorage;
        }

        public InventoryScanner InventoryScanner;
        public ItemStorage ItemStorage;
    }
}