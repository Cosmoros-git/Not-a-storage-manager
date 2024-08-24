using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass
{
    public abstract class ModBase
    {
        protected string ClassName => GetType().Name;

    }
}

