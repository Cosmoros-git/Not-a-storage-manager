using System.Collections.Generic;
using System.Linq;
using Sandbox.Definitions;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class PreLoadGetDefinitions : MySessionComponentBase
    {
        private DictionaryValuesReader<MyDefinitionId, MyDefinitionBase> _allDefinitions;

        public List<MyDefinitionBase> ComponentsDefinitions;
        public List<MyDefinitionBase> OresDefinitions;
        public List<MyDefinitionBase> IngotDefinitions;
        public List<MyDefinitionBase> AmmoDefinition;
        public static PreLoadGetDefinitions Instance { get; private set; }

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