using System;
using System.Collections.Generic;
using System.Linq;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StorageSubclasses;
using ParallelTasks;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.ModAPI;
using MyInventoryItemFilter = Sandbox.ModAPI.Ingame.MyInventoryItemFilter;
using MyConveyorSorterMode = Sandbox.ModAPI.Ingame.MyConveyorSorterMode;


namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.GridAndBlockManagers
{
    internal class ModSorterManager : ModBase
    {
        private readonly HashSet<IMyConveyorSorter> _myConveyorSorter;

        private readonly HashSet<IMyTerminalBlock> _subscribedTerminalBlocks = new HashSet<IMyTerminalBlock>();
        private readonly HashSet<IMyConveyorSorter> _trashSorters;


        public Dictionary<IMyConveyorSorter, Dictionary<MyDefinitionId, ModTuple>> MyItemLimitsCounts =
            new Dictionary<IMyConveyorSorter, Dictionary<MyDefinitionId, ModTuple>>();

        public Dictionary<MyDefinitionId, int> DictionaryTrackedValues = new Dictionary<MyDefinitionId, int>();

        public static Dictionary<IMyConveyorSorter, List<MyInventoryItemFilter>> FilterSorters =
            new Dictionary<IMyConveyorSorter, List<MyInventoryItemFilter>>();

        private readonly HashSet<MyDefinitionId> _changedDefinitions = new HashSet<MyDefinitionId>();


        private const int DefaultAmount = 0;
        private const float DefaultTolerance = 10.0f;
        private readonly ItemStorage _itemStorage;

        public ModSorterManager(HashSet<IMyConveyorSorter> myConveyorSorter)
        {
            _myConveyorSorter = myConveyorSorter;
            _itemStorage = ModAccessStatic.Instance.ItemStorage;
            _trashSorters = myConveyorSorter;

            TrashSorterInitialize(myConveyorSorter);
            ResetConveyorsFilters();


            _itemStorage.ValueChanged += OnValueChanged;
            HeartBeat100 += HeartbeatInstance_HeartBeat100;
        }

        private void HeartbeatInstance_HeartBeat100()
        {
            ProcessChanges();
        }

        private void ResetConveyorsFilters()
        {
            foreach (var sorter in _trashSorters)
            {
                var emptyFilter = new List<MyInventoryItemFilter>();
                sorter.SetFilter(MyConveyorSorterMode.Whitelist, emptyFilter);
            }

            FilterSorters.Clear();
        }


        // Function to add and remove items from filters. Only called. Does nothing else.
        private void AddToConveyorSorterFilter(IMyConveyorSorter sorterIn, MyDefinitionId subtypeId)
        {
            try
            {
                var sorter = sorterIn as MyConveyorSorter;
                if (sorter == null) return; // Ensure sorter is of the expected type

                List<MyInventoryItemFilter> filterList;

                // Synchronize access if this code could be accessed by multiple threads
                lock (FilterSorters)
                {
                    if (!FilterSorters.TryGetValue(sorter, out filterList))
                    {
                        filterList = new List<MyInventoryItemFilter>();
                        sorterIn.GetFilterList(filterList);
                        FilterSorters[sorter] = filterList;
                    }
                }

                // Check if the item is already in the filter list
                if (filterList.Any(item => item.ItemId.Equals(subtypeId))) return;

                filterList.Add(new MyInventoryItemFilter(subtypeId));


                sorterIn.SetFilter(MyConveyorSorterMode.Whitelist, filterList);
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, $"Failed to add to filter: {ex.Message}");
            }
        }

        private void RemoveFromConveyorSorterFilter(IMyConveyorSorter sorterIn, MyDefinitionId subtypeId)
        {
            try
            {
                var sorter = sorterIn as MyConveyorSorter;
                if (sorter == null) return; // Ensure sorter is of the expected type

                List<MyInventoryItemFilter> filterList;

                // Synchronize access if this code could be accessed by multiple threads
                lock (FilterSorters)
                {
                    if (!FilterSorters.TryGetValue(sorter, out filterList))
                    {
                        filterList = new List<MyInventoryItemFilter>();
                        sorterIn.GetFilterList(filterList);
                        FilterSorters[sorter] = filterList;
                    }
                }

                // Check if the item is already in the filter list
                var filterItem = filterList.FirstOrDefault(item => item.ItemId.Equals(subtypeId));

                filterList.Remove(filterItem);


                sorterIn.SetFilter(MyConveyorSorterMode.Whitelist, filterList);
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, $"Failed to remove a filter: {ex.Message}");
            }
        }

        private void TrashSorterInitialize(HashSet<IMyConveyorSorter> myConveyorSorter)
        {
            foreach (var terminal in myConveyorSorter.Cast<IMyTerminalBlock>())
            {
                Subscribe_Terminal_Block(terminal);
                // Should deal with all the previously set filters.
                if (!string.IsNullOrEmpty(terminal.CustomData))
                {
                    Terminal_CustomDataChanged(terminal);
                }
            }
        }

        public void OnTrashSorterAdded(IMyConveyorSorter myConveyorSorter)
        {
            MyAPIGateway.Utilities.ShowMessage(ClassName,
                $"Trash sorter added");
            var terminal = myConveyorSorter as IMyTerminalBlock;
            Subscribe_Terminal_Block(terminal);

            if (!string.IsNullOrEmpty(terminal.CustomData))
            {
                Terminal_CustomDataChanged(terminal);
            }
        }


        public Dictionary<string, ModTuple> ParseAndFillCustomData(IMyTerminalBlock obj)
        {
            MyAPIGateway.Utilities.ShowMessage(ClassName, "Parsing text into RawData");
            // This function is absolute hell show of data parsing.
            var data = obj.CustomData;
            var parsedData = new Dictionary<string, ModTuple>();
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

                MyAPIGateway.Utilities.ShowMessage(ClassName, $"Item name debug {itemDisplayName}");

                // If display name that player gave does not exist in my storage means or I don't care about it. Or its wrong.
                MyDefinitionId definitionId;
                if (!_itemStorage.ContainsKey(itemDisplayName, out definitionId)) continue;

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
                parsedData[itemDisplayName] = new ModTuple(maxAmount, percentageAboveToStartCleanup);
                editedLines.Add($"{itemDisplayName} | {maxAmount} | {percentageAboveToStartCleanup}%");
            }

            // Update the CustomData with the newly formatted lines
            obj.CustomData = string.Join("\n", editedLines);

            return parsedData;
        }

        private void ProcessChanges()
        {
            foreach (var definitionId in _changedDefinitions)
            {
                // Iterate through each sorter in MyItemLimitsCounts
                foreach (var sorterEntry in MyItemLimitsCounts)
                {
                    var sorter = sorterEntry.Key;
                    var limitsDictionary = sorterEntry.Value;

                    // Find the specific sorter filter that matches the definitionId
                    ModTuple modTuple;
                    if (!limitsDictionary.TryGetValue(definitionId, out modTuple)) continue;

                    // Retrieve the current value for this definitionId
                    MyFixedPoint value;
                    if (!_itemStorage.TryGetValue(definitionId, out value)) continue;

                    // Check whether the current value is above or below the limit
                    var result = AboveLimitCheck(modTuple.Limit, modTuple.MaxValue, value);

                    // Handle the different cases based on the limit check
                    switch (result)
                    {
                        case 1: // If the item is above the set limit
                            lock (FilterSorters)
                            {
                                List<MyInventoryItemFilter> filterList;
                                if (!FilterSorters.TryGetValue(sorter, out filterList))
                                {
                                    AddToConveyorSorterFilter(sorter, definitionId);
                                }
                                else if (!filterList.Any(item => item.ItemId.Equals(definitionId)))
                                {
                                    AddToConveyorSorterFilter(sorter, definitionId);
                                }
                            }

                            break;

                        case -1: // If the item is below the set limit
                            lock (FilterSorters)
                            {
                                List<MyInventoryItemFilter> filterList;
                                if (FilterSorters.TryGetValue(sorter, out filterList))
                                {
                                    RemoveFromConveyorSorterFilter(sorter, definitionId);
                                }
                            }

                            break;

                        case 404:
                            MyAPIGateway.Utilities.ShowMessage(ClassName,
                                $"Unexpected state for {definitionId.ToString()} in ProcessChanges.");
                            break;
                    }
                }
            }

            // Clear the set after processing
            _changedDefinitions.Clear();
        }

        private void OnValueChanged(MyDefinitionId definitionId)
        {
            if (!DictionaryTrackedValues.ContainsKey(definitionId)) return;

            // Simply add the changed definitionId to the tracking set
            _changedDefinitions.Add(definitionId);
        }

        private static int AboveLimitCheck(MyFixedPoint limit, MyFixedPoint valueMaxValue, MyFixedPoint value)
        {
            if (value > valueMaxValue) return 1;
            if (value < valueMaxValue && value < limit) return -1;
            return 404;
        }

        public void RawCustomDataTransformer(IMyConveyorSorter sorter, Dictionary<string, ModTuple> rawData)
        {
            MyAPIGateway.Utilities.ShowMessage(ClassName, "Transforming data into system");
            if (sorter == null) return;

            // Ensure the sorter has an entry in MyItemLimitsCounts
            if (!MyItemLimitsCounts.ContainsKey(sorter))
            {
                MyItemLimitsCounts[sorter] = new Dictionary<MyDefinitionId, ModTuple>();
            }

            var convertedData = MyItemLimitsCounts[sorter];

            // Step 1: Process rawData and update or add to convertedData
            foreach (var dataRaw in rawData)
            {
                // Check if I care to track this value.
                MyDefinitionId myDefinitionId;
                if (!_itemStorage.TryGetValue(dataRaw.Key, out myDefinitionId)) continue;


                // Check if the entry is already in convertedData, if it's not there add value counter and set the value.
                // In the case of it being there just update value, don't increase counter.

                if (!convertedData.ContainsKey(myDefinitionId))
                {
                    convertedData[myDefinitionId] = dataRaw.Value;
                    DictionaryTrackedValues[myDefinitionId] += 1;
                }
                else
                {
                    convertedData[myDefinitionId] = dataRaw.Value;
                }
            }

            // Step 2: Remove Definitions that are no longer in the custom data.
            var keysToRemove =
                (from data in convertedData where !rawData.ContainsKey(data.Key.SubtypeName) select data.Key).ToList();

            // Actually remove the keys that were marked for removal
            foreach (var key in keysToRemove)
            {
                convertedData.Remove(key);
                //This updates the counter so I don't even know why it exists atm.
                DictionaryTrackedValues[key] = -1;
            }
        }


        private void Terminal_CustomDataChanged(IMyTerminalBlock obj)
        {
            MyAPIGateway.Utilities.ShowMessage(ClassName, "Terminal data Changed");
            if (!_trashSorters.Contains(obj)) return;

            // Unsubscribe before making changes
            Unsubscribe_Terminal_Block(obj);

            // Parse and update CustomData
            var data = ParseAndFillCustomData(obj);
            var sorter = obj as IMyConveyorSorter;
            if (sorter != null)
            {
                RawCustomDataTransformer(sorter, data);
            }

            // Resubscribe after changes are made
            Subscribe_Terminal_Block(obj);
        }

        private void Unsubscribe_Terminal_Block(IMyTerminalBlock terminal)
        {
            if (terminal == null)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName,
                    "Unsubscribe_Terminal_Block: Attempted to unsubscribe a null terminal block.");
                return; // Safeguard: Check if terminal is null
            }

            try
            {
                if (_subscribedTerminalBlocks.Contains(terminal))
                {
                    terminal.CustomDataChanged -= Terminal_CustomDataChanged;
                    _subscribedTerminalBlocks.Remove(terminal);
                }
                else
                {
                    MyAPIGateway.Utilities.ShowMessage(ClassName,
                        $"Unsubscribe_Terminal_Block: Terminal block {terminal.CustomName} was not found in the subscribed list.");
                }
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName,
                    $"Error unsubscribing terminal block {terminal.CustomName}: {ex.Message}");
            }
        }


        private void Subscribe_Terminal_Block(IMyTerminalBlock terminal)
        {
            if (_subscribedTerminalBlocks.Contains(terminal)) return;
            terminal.CustomDataChanged += Terminal_CustomDataChanged;
            terminal.OnClosing += Terminal_OnClosing;
            _subscribedTerminalBlocks.Add(terminal);
        }


        private void Terminal_OnClosing(IMyEntity obj)
        {
            if (obj == null) return; // Safeguard: Check if obj is null

            try
            {
                var terminalBlock = obj as IMyTerminalBlock;
                if (terminalBlock != null)
                {
                    Unsubscribe_Terminal_Block(terminalBlock);
                }
                else
                {
                    MyAPIGateway.Utilities.ShowMessage(ClassName,
                        "Terminal_OnClosing: Unable to unsubscribe terminal block as it was null or of wrong type.");
                }

                var sorter = obj as IMyConveyorSorter;
                if (sorter != null)
                {
                    // Remove the sorter from relevant collections
                    MyItemLimitsCounts.Remove(sorter);
                    _trashSorters.Remove(sorter);
                    ClearWatchedListOfTerminal(sorter);
                }
                else
                {
                    MyAPIGateway.Utilities.ShowMessage(ClassName,
                        "Terminal_OnClosing: Object is not a valid IMyConveyorSorter.");
                }
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, $"Error in Terminal_OnClosing: {ex.Message}");
            }
        }


        private void ClearWatchedListOfTerminal(IMyConveyorSorter sorter)
        {
            Dictionary<MyDefinitionId, ModTuple> count;
            if (!MyItemLimitsCounts.TryGetValue(sorter, out count) || count == null || count.Count <= 0)
                return;

            foreach (var item in count.Where(item => DictionaryTrackedValues.ContainsKey(item.Key)))
            {
                DictionaryTrackedValues[item.Key] -= 1;
            }
        }

        public override void Dispose()
        {
            try
            {
                // Safeguard: Check if _subscribedTerminalBlocks is not null before iterating
                if (_subscribedTerminalBlocks != null)
                {
                    foreach (var terminal in _subscribedTerminalBlocks)
                    {
                        try
                        {
                            if (terminal == null) continue;
                            Unsubscribe_Terminal_Block(terminal);
                            terminal.OnClosing -= Terminal_OnClosing;
                        }
                        catch (Exception ex)
                        {
                            MyAPIGateway.Utilities.ShowMessage(ClassName,
                                $"Error unsubscribing terminal block {terminal?.CustomName}: {ex}");
                        }
                    }
                }

                // Safeguard: Check if _itemStorage is not null before unsubscribing from ValueChanged event
                if (_itemStorage != null)
                {
                    try
                    {
                        _itemStorage.ValueChanged -= OnValueChanged;
                    }
                    catch (Exception ex)
                    {
                        MyAPIGateway.Utilities.ShowMessage(ClassName,
                            $"Error unsubscribing from ValueChanged event: {ex}");
                    }
                }

                // Safeguard: Check if collections are not null before clearing
                _trashSorters?.Clear();
                _subscribedTerminalBlocks?.Clear();
                _myConveyorSorter?.Clear();
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName,
                    $"Error in Dispose method: {ex}");
            }
        }
    }
}