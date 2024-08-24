using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class GetDefinitions : MySessionComponentBase
    {
        private DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> _allDefinitions;

        public List<MyDefinitionBase> ComponentsDefinitions;
        public List<MyDefinitionBase> OresDefinitions;
        public List<MyDefinitionBase> IngotDefinitions;
        public List<MyDefinitionBase> AmmoDefinition;
        public static GetDefinitions Instance { get; private set; }

        public override void LoadData()
        {
            base.LoadData();
            _allDefinitions = MyDefinitionManager.Static.GetAllDefinitions();
            GetBasicObjects();
            Instance = this;
        }


        public void GetBasicObjects()
        {
            ComponentsDefinitions =
                _allDefinitions.Where(d => d.Id.TypeId == typeof(MyObjectBuilder_Component)).ToList();
            OresDefinitions = _allDefinitions.Where(d => d.Id.TypeId == typeof(MyObjectBuilder_Ore)).ToList();
            IngotDefinitions = _allDefinitions.Where(d => d.Id.TypeId == typeof(MyObjectBuilder_Ingot)).ToList();
            AmmoDefinition = _allDefinitions.Where(d => d.Id.TypeId == typeof(MyObjectBuilder_AmmoMagazine)).ToList();
            Instance = this;
        }
    }
}