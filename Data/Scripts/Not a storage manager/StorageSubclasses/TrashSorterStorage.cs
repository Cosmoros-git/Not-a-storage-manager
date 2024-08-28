using System;
using System.Collections.Generic;
using System.Linq;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.StorageSubclasses
{
    public class TrashSorterStorage : ModBase
    {
        public readonly HashSet<IMyConveyorSorter> TrashSorters = new HashSet<IMyConveyorSorter>();
        private readonly HashSet<IMyTerminalBlock> _subscribedTerminals = new HashSet<IMyTerminalBlock>();
        private const int DefaultAmount = 0;
        private const float DefaultTolerance = 10.0f;
        private readonly ItemStorage _itemStorage = ModAccessStatic.Instance.ItemStorage;

        public bool Add(IMyCubeBlock block)
        {
            var sorter = block as IMyConveyorSorter;
            if (sorter == null) return false;
            if (!TrashSubtype.Contains(sorter.BlockDefinition.SubtypeId)) return false;
            TrashSorters.Add(sorter);
            RegisterTerminal(block);
            block.OnClosing += Block_OnClosing;
            return true;
        }
        public bool Remove(IMyCubeBlock block)
        {
            var sorter = block as IMyConveyorSorter;
            if (sorter == null) return false;
            if (!TrashSorters.Contains(sorter)) return false;
            TrashSorters.Remove(sorter);
            return true;
        }



        public void ForceUpdateAllSorters()
        {
            foreach (var sorter in TrashSorters)
            {
                Terminal_CustomDataChanged(sorter);
            }
        }
        private void Terminal_CustomDataChanged(IMyTerminalBlock obj)
        {
            MyAPIGateway.Utilities.ShowMessage(ClassName, "Terminal data Changed");
            if (!TrashSorters.Contains(obj)) return;

            // Unsubscribe before making changes
            UnRegisterTerminal(obj);

            // Parse and update CustomData
            if (obj.CustomData.Contains("[FillMe]"))
            {
                obj.CustomData = _itemStorage.GetDisplayNameListAsCustomData();
            }
            var data = ParseAndFillCustomData(obj);
            var sorter = obj as IMyConveyorSorter;
            if (sorter != null)
            {
                RawCustomDataTransformer(sorter, data);
            }

            // Resubscribe after changes are made
            RegisterTerminal(obj);
        }

        public void RawCustomDataTransformer(IMyConveyorSorter sorter, Dictionary<string, ModTuple> rawData)
        {
            var access = ModAccessStatic.Instance.ItemLimitsStorage;
            MyAPIGateway.Utilities.ShowMessage(ClassName, "Transforming data into system");
            if (sorter == null) return;

            // Ensure the sorter has an entry in MyItemLimitsCounts
            if (!access.MyItemLimitsCounts.ContainsKey(sorter))
            {
                access.MyItemLimitsCounts[sorter] = new Dictionary<MyDefinitionId, ModTuple>();
            }

            var convertedData = access.MyItemLimitsCounts[sorter];

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
                    access.DictionaryTrackedValues[myDefinitionId] += 1;
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
                access.DictionaryTrackedValues[key] = -1;
            }
        }
        private Dictionary<string, ModTuple> ParseAndFillCustomData(IMyTerminalBlock obj)
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
        
        private static void TrackedValuesRemoveItems(IMyConveyorSorter sorter)
        {
            var access = ModAccessStatic.Instance.ItemLimitsStorage;
            Dictionary<MyDefinitionId, ModTuple> count;
            if (!access.MyItemLimitsCounts.TryGetValue(sorter, out count) || count == null || count.Count <= 0)
                return;

            foreach (var item in count.Where(item => access.DictionaryTrackedValues.ContainsKey(item.Key)))
            {
                access.DictionaryTrackedValues[item.Key] -= 1;
            }
        }

        private void RegisterTerminal(IMyCubeBlock block)
        {
            var terminal = block as IMyTerminalBlock;
            if (terminal != null)
            {
                if (_subscribedTerminals.Contains(terminal)) return;
                terminal.CustomDataChanged += Terminal_CustomDataChanged;
                if (!string.IsNullOrEmpty(terminal.CustomData))
                {
                    Terminal_CustomDataChanged(terminal);
                }
            }
            else
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, "No idea how this specific sorter has no terminal");
            }

            _subscribedTerminals.Add(terminal);
        }
        private void UnRegisterTerminal(IMyCubeBlock block)
        {
            var terminal = block as IMyTerminalBlock;
            if (terminal != null)
            {
                if (!_subscribedTerminals.Contains(terminal)) return;
                terminal.CustomDataChanged -= Terminal_CustomDataChanged;
            }
            else
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, "No idea how this specific sorter has no terminal");
            }

            _subscribedTerminals.Add(terminal);
        }


        private void Block_OnClosing(VRage.ModAPI.IMyEntity obj)
        {
            Remove(obj as IMyConveyorSorter);
            UnRegisterTerminal(obj as IMyCubeBlock);
            TrackedValuesRemoveItems(obj as IMyConveyorSorter);
        }
    }
}