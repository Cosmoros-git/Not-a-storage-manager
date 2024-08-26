using NotAStorageManager.Data.Scripts.Not_a_storage_manager.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses
{
    public class GlobalStorageInstance
    {
        public static GlobalStorageInstance Instance { get; set; }

        public GlobalStorageInstance()
        {
            Instance = this;
        }

        public MyInventories MyInventories = new MyInventories();
    }
}