using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.CustomDataManager;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses;
using Sandbox.ModAPI.Ingame;
using VRage;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using IMyConveyorSorter = Sandbox.ModAPI.IMyConveyorSorter;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;


namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.GridAndBlockManagers
{
    internal class ConveyorSorterManager : HeartbeatCore, IDisposable
    {
        private readonly HashSet<IMyConveyorSorter> _myConveyorSorter;

        private readonly Dictionary<string, string> _dictionaryDisplayNameToSubtype;
        private readonly Dictionary<string, MyFixedPoint> _inventoryDictionaryInFixedPoint;

        public const string ClassName = "ConveyorSorterManager";

        private readonly HashSet<IMyTerminalBlock> _subscribedTerminalBlocks = new HashSet<IMyTerminalBlock>();
        private readonly HashSet<IMyConveyorSorter> _trashSorters;

        public Dictionary<IMyConveyorSorter, Dictionary<string, TotallyATuple>> MyItemLimitsRaw =
            new Dictionary<IMyConveyorSorter, Dictionary<string, TotallyATuple>>();

        public Dictionary<IMyConveyorSorter, Dictionary<string, TotallyATuple>> MyItemLimitsCounts =
            new Dictionary<IMyConveyorSorter, Dictionary<string, TotallyATuple>>();

        public List<string> WatchedListRaw = new List<string>();
        public HashSet<string> WatchedListUnique = new HashSet<string>();
        public HashSet<string> CurrentlyWatchedListUnique = new HashSet<string>();

        private const int DefaultAmount = 0;
        private const float DefaultTolerance = 10.0f;

        public ConveyorSorterManager(HashSet<IMyConveyorSorter> myConveyorSorter)
        {
            _myConveyorSorter = myConveyorSorter;
            var access = GlobalStorageInstance.Instance.MyInventories.InventoriesData;
            _dictionaryDisplayNameToSubtype = access.Dictionary_Display_Name_To_ObjectBuilder_TypeId;
            _inventoryDictionaryInFixedPoint = access.Storage_Dictionary;
            _trashSorters = myConveyorSorter;
            Subscribe_All_Trash_Sorters(myConveyorSorter);
            HeartbeatInstance.HeartBeat1000 += HeartbeatInstance_HeartBeat1000;
            HeartbeatInstance.HeartBeat100 += HeartbeatInstance_HeartBeat100;
        }

        private void HeartbeatInstance_HeartBeat100()
        {
            foreach (var id in CurrentlyWatchedListUnique)
            {
                // Get the current value from the inventory dictionary
                MyFixedPoint value;
                if (!_inventoryDictionaryInFixedPoint.TryGetValue(id, out value))
                {
                    continue; // Skip if the ID is not found in the dictionary
                }

                // Iterate over each sorter and its associated limits
                foreach (var sorterEntry in MyItemLimitsCounts)
                {
                    var sorter = sorterEntry.Key;
                    var limits = sorterEntry.Value;

                    // Check if the current ID has a corresponding limit in the sorter
                    TotallyATuple watchList;
                    if (!limits.TryGetValue(id, out watchList)) continue;

                    // Compare the inventory value with the defined limit
                    if (value >= watchList.Limit)
                    {
                        // If the value exceeds the limit, ensure the item is added to the filter
                        AddToConveyorSorterFilter(sorter, id);
                    }
                    else
                    {
                        // If the value is below the limit, remove the item from the filter
                        RemoveFromConveyorSorterFilter(sorter, id);
                    }
                }
            }
        }

        // Function to add an item to the ConveyorSorter filter
        private void AddToConveyorSorterFilter(IMyConveyorSorter sorter, string subtypeId)
        {
            var filterList = new List<MyInventoryItemFilter>();
            sorter.GetFilterList(filterList);
            if (filterList.Any(item => item.ItemId.SubtypeId.String == subtypeId)) return;
            var filterItem = new MyInventoryItemFilter(subtypeId);
            filterList.Add(filterItem);
            sorter.SetFilter(MyConveyorSorterMode.Whitelist, filterList);
        }

        // Function to remove an item from the ConveyorSorter filter
        private void RemoveFromConveyorSorterFilter(IMyConveyorSorter sorter, string subtypeId)
        {
            var filterList = new List<MyInventoryItemFilter>();
            sorter.GetFilterList(filterList);

            // Find the item in the filter list
            var filterItem = filterList.FirstOrDefault(item => item.ItemId.SubtypeId.String == subtypeId);

            filterList.Remove(filterItem);
            sorter.SetFilter(MyConveyorSorterMode.Whitelist, filterList); // Assuming you want to maintain the whitelist mode
        }


        private void HeartbeatInstance_HeartBeat1000()
        {
            foreach (var id in WatchedListUnique)
            {
                var value = _inventoryDictionaryInFixedPoint[id];
                foreach (var x in MyItemLimitsCounts.Select(item => item.Value))
                {
                    TotallyATuple watchList;
                    if (!x.TryGetValue(id, out watchList)) continue;
                    if (value > watchList.MaxValue) CurrentlyWatchedListUnique.Add(id);
                }
            }
        }

        private void Subscribe_All_Trash_Sorters(HashSet<IMyConveyorSorter> myConveyorSorter)
        {
            foreach (var terminal in myConveyorSorter.Cast<IMyTerminalBlock>())
            {
                Subscribe_Terminal_Block(terminal);
                terminal.OnClosing += Terminal_OnClosing;
                // Should deal with all the previously set filters.
                if (!string.IsNullOrEmpty(terminal.CustomData))
                {
                    Terminal_CustomDataChanged(terminal);
                }
            }
        }


        public Dictionary<string, TotallyATuple> ParseAndFillCustomData(IMyTerminalBlock obj)
        {
            // This function is absolute hell show of data parsing.
            var data = obj.CustomData;
            var parsedData = new Dictionary<string, TotallyATuple>();
            var lines = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var editedLines = new List<string>();

            // This is for future me. First of all why these 4 variables? First one used to parse into data into folder and be edited back and spat out.
            // Parsed data is the freaky class that I pass with display name keys values and limits at which to start cleanup.
            // lines no one cares. Its the text in lines
            // edited lines is the data that will populate the custom data of the block. It removes trash out of it and populates it for easier fill.

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim())
                    .ToArray();
                var itemDisplayName = parts[0];

                // If display name that player gave does not exist in my storage means or I don't care about it. Or its wrong.
                if (!_dictionaryDisplayNameToSubtype.ContainsKey(itemDisplayName)) continue;

                // If DisplayName is already in the dictionary, skip processing
                if (parsedData.ContainsKey(itemDisplayName))
                    continue;

                // Defaults if anything messes up
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

        public void RawLimitsTransformer(IMyConveyorSorter sorter)
        {
            // This never should be true, but if it somehow happens. There is a check.
            if (sorter == null || !MyItemLimitsRaw.ContainsKey(sorter)) return;

            // Ensure the sorter has an entry in MyItemCounts
            if (!MyItemLimitsCounts.ContainsKey(sorter))
            {
                MyItemLimitsCounts[sorter] = new Dictionary<string, TotallyATuple>();
            }

            var convertedData = MyItemLimitsCounts[sorter];
            var rawData = MyItemLimitsRaw[sorter];
            foreach (var data in rawData)
            {
                var id = _dictionaryDisplayNameToSubtype[data.Key];
                convertedData[id] = data.Value;

                // This is a way for me to not check ALL ITEMS.
                WatchedListRaw.Add(id);
            }

            WatchedListUnique = new HashSet<string>(WatchedListRaw);
        }

        private void Terminal_CustomDataChanged(IMyTerminalBlock obj)
        {
            if (!_trashSorters.Contains(obj)) return;

            // Unsubscribe before making changes
            Unsubscribe_Terminal_Block(obj);

            // Parse and update CustomData
            var data = ParseAndFillCustomData(obj);
            var sorter = obj as IMyConveyorSorter;
            if (sorter != null)
            {
                MyItemLimitsRaw[sorter] = data;
                RawLimitsTransformer(sorter);
            }

            // Resubscribe after changes are made
            Subscribe_Terminal_Block(obj);
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

        private void Terminal_OnClosing(IMyEntity obj)
        {
            Unsubscribe_Terminal_Block(obj as IMyTerminalBlock);
            var sorter = (IMyConveyorSorter)obj;
            MyItemLimitsRaw.Remove(sorter);
            MyItemLimitsCounts.Remove(sorter);
            _trashSorters.Remove(sorter);
            ClearWatchedListOfTerminal(sorter);
        }

        private void ClearWatchedListOfTerminal(IMyConveyorSorter sorter)
        {
            if (MyItemLimitsCounts[sorter].Count <= 0) return;
            foreach (var item in MyItemLimitsCounts[sorter])
            {
                WatchedListRaw.Remove(item.Key);
            }

            WatchedListUnique = new HashSet<string>(WatchedListRaw);
        }

        public void Dispose()
        {
            foreach (var terminal in _subscribedTerminalBlocks)
            {
                Unsubscribe_Terminal_Block(terminal);
                terminal.OnClosing -= Terminal_OnClosing;
            }

            _trashSorters.Clear();
            _subscribedTerminalBlocks.Clear();
            _myConveyorSorter.Clear();
            WatchedListRaw.Clear();
        }
    }
}