using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass
{
    public class CustomData
    {
        public CustomData(string customData, IMyTerminalBlock iMyTerminalBlock)
        {
            _customDataString = customData;
            ModTerminalBlock = iMyTerminalBlock;
        }

        private string _customDataString;
        private int _customDataSize;
        private int _customDataChecksum;

        public Action<IMyTerminalBlock, string> CustomDataChanged;

        public string CustomDataString
        {
            get { return _customDataString; }
            set
            {
                if (ReferenceEquals(_customDataString, value)) return; // Early exit if both references are the same

                var newSize = value.Length;
                var newChecksum = ComputeChecksum(value);

                if (_customDataSize == newSize && _customDataChecksum == newChecksum && _customDataString.Equals(value))
                {
                    return; // No change detected, exit early
                }

                _customDataString = value;
                _customDataSize = newSize;
                _customDataChecksum = newChecksum;
                CustomDataChanged?.Invoke(ModTerminalBlock, value);
            }
        }

        private static int ComputeChecksum(string data)
        {
            if (string.IsNullOrEmpty(data)) return 0;

            const int prime = 31;
            return data.Aggregate(0, (current, t) => current * prime + t);
        }

        public IMyTerminalBlock ModTerminalBlock { get; set; }
    }

    internal class CustomDataManager
    {
        public HashSet<IMyCubeBlock> ManagedBlocks = new HashSet<IMyCubeBlock>();
        public CustomDataManager() { }
    }
}