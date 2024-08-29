using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using VRage.Game;
using VRage;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.StorageSubclasses
{
    public class ItemDefinitionStorage:ModBase
    {
        private readonly Dictionary<MyDefinitionId, string> _definitionIdToName =
            new Dictionary<MyDefinitionId, string>();

        private readonly Dictionary<string, MyDefinitionId> _nameToDefinitionId =
            new Dictionary<string, MyDefinitionId>();

        private readonly Dictionary<MyDefinitionId, MyFixedPoint> _definitionIdToFixedPoint =
            new Dictionary<MyDefinitionId, MyFixedPoint>();

        private readonly List<string> _entryNames = new List<string>();

        public event Action<MyDefinitionId> ValueChanged;
        // Add methods to interact with both dictionaries

        public void Add(string displayName, MyDefinitionId definitionId, MyFixedPoint fixedPoint)
        {
            _nameToDefinitionId[displayName] = definitionId;
            _definitionIdToName[definitionId] = displayName;
            _definitionIdToFixedPoint[definitionId] = fixedPoint;
            _entryNames.Add(displayName);
        }



        public bool ContainsKey(string displayName)
        {
            MyDefinitionId definitionId;
            return _nameToDefinitionId.TryGetValue(displayName, out definitionId) && _definitionIdToFixedPoint.ContainsKey(definitionId);
        }
        public bool ContainsKey(string displayName, out MyDefinitionId definitionId)
        {
            if (_nameToDefinitionId.TryGetValue(displayName, out definitionId))
            {
                return _definitionIdToFixedPoint.ContainsKey(definitionId);
            }

            // If displayName does not exist, return false and set definitionId to default
            definitionId = default(MyDefinitionId);
            return false;
        }
        public bool ContainsKey(MyDefinitionId definitionId)
        {
            return _definitionIdToFixedPoint.ContainsKey(definitionId);
        }


        public bool TryGetValue(string displayName, out MyDefinitionId definitionId)
        {
            return _nameToDefinitionId.TryGetValue(displayName, out definitionId);
        }

        public bool TryGetValue(MyDefinitionId definitionId, out string modDisplayName)
        {
            string value;
            if (!_definitionIdToName.TryGetValue(definitionId, out value))
            {
                modDisplayName = default(string);
                return false;

            }
            modDisplayName = value;
            return true;
        }
        public bool TryGetValue(MyDefinitionId definitionId, out MyFixedPoint fixedPoint)
        {
            return _definitionIdToFixedPoint.TryGetValue(definitionId, out fixedPoint);
        }

       
        public MyFixedPoint TryUpdateValue(string displayName, MyFixedPoint value)
        {
            MyDefinitionId definitionId;
            if (!_nameToDefinitionId.TryGetValue(displayName, out definitionId)) return -1;

            OnValueUpdated(definitionId);
            return TryUpdateValue(definitionId, value);


        }
        public MyFixedPoint TryUpdateValue(MyDefinitionId definitionId, MyFixedPoint value)
        {
            if (!_definitionIdToFixedPoint.ContainsKey(definitionId)) return -1;
                OnValueUpdated(definitionId);
            return _definitionIdToFixedPoint[definitionId] += value;

        }



        public MyFixedPoint GetFixedPointFromDisplayName(string displayName)
        {
            MyDefinitionId definitionId;
            if (!_nameToDefinitionId.TryGetValue(displayName, out definitionId))
                return -1; // or handle not found case as needed
            MyFixedPoint fixedPoint;
            return _definitionIdToFixedPoint.TryGetValue(definitionId, out fixedPoint) ? fixedPoint : -1;
        }
        public string GetDisplayNameListAsCustomData()
        {
            return string.Join("\n", _entryNames);
        }





        protected virtual void OnValueUpdated(MyDefinitionId obj)
        {
            ValueChanged?.Invoke(obj);
        }

    }
}