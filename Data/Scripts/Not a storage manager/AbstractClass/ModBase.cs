using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass
{
    public abstract class ModBase
    {
        protected string ClassName => GetType().Name;
        protected const string TrashIdentifier = "[TRASH]";
        protected static readonly Dictionary<string, string> EdgeCases = new Dictionary<string, string>()
        {
            { "MyObjectBuilder_Ore", "Ore_" },
            { "MyObjectBuilder_Ingot", "Ingot_" }
        };

        protected static readonly HashSet<string> CountedTypes = new HashSet<string>()
        {
            "MyObjectBuilder_Ingot",
            "MyObjectBuilder_Ore",
            "MyObjectBuilder_Component",
            "MyObjectBuilder_AmmoMagazine"
        };
    }
}

