using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.ModAPI;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.GridAndBlockManagers
{
    internal class TotallyATuple
    {
        public int Limit { get; private set; }
        public float ToleranceRange { get; private set; }
        public MyFixedPoint MinValue { get; private set; }
        public MyFixedPoint MaxValue { get; private set; }

        public TotallyATuple(int limit, float toleranceRange)
        {
            Limit = limit;
            ToleranceRange = toleranceRange < 0 ? 0 : toleranceRange;

            var tolerance = limit * ToleranceRange / 100;

            MinValue = (MyFixedPoint)(limit - tolerance);
            MaxValue = (MyFixedPoint)(limit + tolerance);
        }
    }

    internal class ConveyorSorterManager : HeartbeatCore, IDisposable
    {
        private readonly HashSet<IMyConveyorSorter> _myConveyorSorter;
        private readonly Dictionary<string, string> _dictionarySubtypeToDisplayName;
        private readonly Dictionary<string, MyFixedPoint> _dictionarySubtypeIdToMyFixedPoint;

        public const string ClassName = "ConveyorSorterManager";

        private List<IMyTerminalBlock> _subscribedTerminalBlocks = new List<IMyTerminalBlock>();
        private readonly List<IMyConveyorSorter> _trashSorters = new List<IMyConveyorSorter>();

        public Dictionary<string, TotallyATuple> MyItemLimitStorage = new Dictionary<string, TotallyATuple>();

        const int DefaultAmount = 0;
        const float DefaultTolerance = 10.0f;

        public ConveyorSorterManager(HashSet<IMyConveyorSorter> myConveyorSorter)
        {
            this._myConveyorSorter = myConveyorSorter;
            var access = GlobalStorageInstance.Instance.MyInventories.InventoriesData;
            _dictionarySubtypeToDisplayName = access.DictionarySubtypeToDisplayName;
            _dictionarySubtypeIdToMyFixedPoint = access.DictionarySubtypeToMyFixedPoint;
            Subscribe_All_Trash_Sorters(myConveyorSorter);
        }

        private void Subscribe_All_Trash_Sorters(HashSet<IMyConveyorSorter> myConveyorSorter)
        {
            foreach (var terminal in myConveyorSorter.Cast<IMyTerminalBlock>())
            {
                _subscribedTerminalBlocks.Add(terminal);
                terminal.CustomDataChanged += Terminal_CustomDataChanged;

                // Should deal with all the previously set filters.
                if (!string.IsNullOrEmpty(terminal.CustomData))
                {
                    Terminal_CustomDataChanged(terminal);
                }
            }
        }

        private void Terminal_CustomDataChanged(IMyTerminalBlock obj)
        {
            if (!_trashSorters.Contains(obj)) return;

            // Unsubscribe before making changes
            Unsubscribe_Terminal_Block(obj);

            // Parse and update CustomData
            ParseAndFillCustomData(obj);

            // Resubscribe after changes are made
            Subscribe_Terminal_Block(obj);
        }

        public Dictionary<string, TotallyATuple> ParseAndFillCustomData(IMyTerminalBlock obj)
        {
            var data = obj.CustomData;
            var parsedData = new Dictionary<string, TotallyATuple>();
            var lines = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var editedLines = new List<string>();

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();
                var itemDisplayName = parts[0];

                // If DisplayName is already in the dictionary, skip processing
                if (parsedData.ContainsKey(itemDisplayName)) continue;

                var maxAmount = DefaultAmount;
                var percentageAboveToStartCleanup = DefaultTolerance;

                if (parts.Length == 3)
                {
                    // Try to parse the amount
                    if (!int.TryParse(parts[1], out maxAmount))
                    {
                        maxAmount = DefaultAmount;
                    }

                    // Try to parse the percentage
                    if (!float.TryParse(parts[2].TrimEnd('%'), out percentageAboveToStartCleanup))
                    {
                        percentageAboveToStartCleanup = DefaultTolerance;
                    }
                }

                // Add to dictionary and format the line for CustomData
                parsedData[itemDisplayName] = new TotallyATuple(maxAmount, percentageAboveToStartCleanup);
                editedLines.Add($"{itemDisplayName} | {maxAmount} | {percentageAboveToStartCleanup}%");
            }

            // Update the CustomData with the newly formatted lines
            obj.CustomData = string.Join("\n", editedLines);

            return parsedData;
        }

        private void Unsubscribe_Terminal_Block(IMyTerminalBlock terminal)
        {
            if (!_subscribedTerminalBlocks.Contains(terminal)) return;
            terminal.CustomDataChanged -= Terminal_CustomDataChanged;
            _subscribedTerminalBlocks.Remove(terminal);
        }

        private void Subscribe_Terminal_Block(IMyTerminalBlock terminal)
        {
            if (_subscribedTerminalBlocks.Contains(terminal)) return;
            terminal.CustomDataChanged += Terminal_CustomDataChanged;
            _subscribedTerminalBlocks.Add(terminal);
        }

        public void Dispose()
        {
            foreach (var terminal in _subscribedTerminalBlocks)
            {
                Unsubscribe_Terminal_Block(terminal);
            }
            _trashSorters.Clear();
            _subscribedTerminalBlocks.Clear();
            _myConveyorSorter.Clear();
        }
    }
}