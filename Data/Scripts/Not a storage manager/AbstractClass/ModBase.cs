using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass
{
    public abstract class ModBase:ModHeartRate
    {
        protected string ClassName => GetType().Name;
        protected const string TrashIdentifier = "[TRASH]";

        protected static readonly HashSet<string> CountedTypes = new HashSet<string>()
        {
            "MyObjectBuilder_Ingot",
            "MyObjectBuilder_Ore",
            "MyObjectBuilder_Component",
            "MyObjectBuilder_AmmoMagazine"
        };

        protected static readonly HashSet<string> NamingExceptions = new HashSet<string>()
        {
            "Stone",
            "Ice",
            "Crude Oil",
            "Coal",
            "Scrap Metal",
        };

        protected static readonly HashSet<string> UniqueModExceptions = new HashSet<string>()
        {
            "Heat",
        };
    }
}