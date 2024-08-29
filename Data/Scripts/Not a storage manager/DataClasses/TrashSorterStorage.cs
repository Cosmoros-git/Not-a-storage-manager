using System;
using System.Collections.Generic;
using System.Linq;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.NoIdeaHowToNameFiles;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StorageSubclasses;
using VRage.Game;
using VRage.Game.ModAPI;
using IMyConveyorSorter = Sandbox.ModAPI.IMyConveyorSorter;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.DataClasses
{
    public class TrashSorterStorage : ModBase
    {
        public readonly HashSet<IMyConveyorSorter> TrashSorters = new HashSet<IMyConveyorSorter>();
        private readonly HashSet<IMyTerminalBlock> _subscribedTerminals = new HashSet<IMyTerminalBlock>();
        private const int DefaultAmount = 0;
        private const float DefaultTolerance = 10.0f;
        private readonly ItemDefinitionStorage _itemDefinitionStorage;

        private readonly Dictionary<IMyTerminalBlock, CustomData> _customDataStorage =
            new Dictionary<IMyTerminalBlock, CustomData>();

        private readonly ItemLimitsStorage _itemLimitsStorage;


        public TrashSorterStorage(ItemDefinitionStorage itemDefinitionStorage, ItemLimitsStorage itemLimitsStorage)
        {
            _itemDefinitionStorage = itemDefinitionStorage;
            _itemLimitsStorage = itemLimitsStorage;
            HeartBeat100 += TrashSorterStorage_HeartBeat100;
        }

        private void TrashSorterStorage_HeartBeat100()
        {
            //ModLogger.Instance.Log(ClassName,$"OnHeartbeat 100 scan, amount of checked items {_customDataStorage.Count}");
            foreach (var data in _customDataStorage)
            {
                var newData = data.Key.CustomData;
                data.Value.CustomDataString = newData;
            }
        }

        public bool Add(IMyCubeBlock block)
        {
            var sorter = block as IMyConveyorSorter;
            if (sorter == null) return false;
            if (!TrashSubtype.Contains(sorter.BlockDefinition.SubtypeId)) return false;
            TrashSorters.Add(sorter);
            ModLogger.Instance.Log(ClassName, "Trash sorter added");
            RegisterTerminal((IMyTerminalBlock)block);
            block.OnClosing += Block_OnClosing;
            return true;
        }

        public bool Remove(IMyCubeBlock block)
        {
            var sorter = block as IMyConveyorSorter;
            if (sorter == null) return false;
            if (!TrashSorters.Contains(sorter)) return false;
            TrashSorters.Remove(sorter);
            UnRegisterTerminal((IMyTerminalBlock)block);
            ModLogger.Instance.Log(ClassName, "Trash sorter removed");
            return true;
        }


        public void ForceUpdateAllSorters()
        {
            ModLogger.Instance.LogWarning(ClassName, $"Forcing all sorters to update {TrashSorters.Count}");
            foreach (var sorter in _customDataStorage)
            {
                Terminal_CustomDataChanged(sorter.Key, sorter.Value.CustomDataString);
            }
        }

        private void Terminal_CustomDataChanged(IMyTerminalBlock obj, string s)
        {
            ModLogger.Instance.Log(ClassName, "Terminal data Changed");
            if (!TrashSorters.Contains(obj)) return;
            if (string.IsNullOrEmpty(obj.CustomData))
            {
                ModLogger.Instance.Log(ClassName, "Custom data is empty. Returning");
                return;
            }

            // Parse and update CustomData
            var tempData = obj.CustomData.ToLowerInvariant();
            ModLogger.Instance.Log(ClassName, $"Temp data is {tempData}");
            if (tempData.Contains(FillMeTag))
            {
                ModLogger.Instance.Log(ClassName, "Fill Me tag detected");
                var FillMeData = _itemDefinitionStorage.GetDisplayNameListAsCustomData();
                ModLogger.Instance.Log(ClassName, FillMeData);
                return;
            }

            var data = ParseAndFillCustomData(obj);
            var sorter = obj as IMyConveyorSorter;
            RawCustomDataTransformer(sorter, data);
        }


        public void RawCustomDataTransformer(IMyConveyorSorter sorter, Dictionary<string, ModTuple> rawData)
        {
            ModLogger.Instance.Log(ClassName, "Transforming data into system");
            if (sorter == null) return;

            // Ensure the sorter has an entry in MyItemLimitsCounts
            if (!_itemLimitsStorage.MyItemLimitsCounts.ContainsKey(sorter))
            {
                _itemLimitsStorage.MyItemLimitsCounts[sorter] = new Dictionary<MyDefinitionId, ModTuple>();
            }

            var convertedData = _itemLimitsStorage.MyItemLimitsCounts[sorter];

            // Step 1: Process rawData and update or add to convertedData
            foreach (var dataRaw in rawData)
            {
                // Check if I care to track this value.
                MyDefinitionId myDefinitionId;
                if (!_itemDefinitionStorage.TryGetValue(dataRaw.Key, out myDefinitionId)) continue;


                // Check if the entry is already in convertedData, if it's not there add value counter and set the value.
                // In the case of it being there just update value, don't increase counter.

                if (!convertedData.ContainsKey(myDefinitionId))
                {
                    convertedData[myDefinitionId] = dataRaw.Value;
                    _itemLimitsStorage.DictionaryTrackedValues[myDefinitionId] += 1;
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
                _itemLimitsStorage.DictionaryTrackedValues[key] = -1;
            }
        }

        private Dictionary<string, ModTuple> ParseAndFillCustomData(IMyTerminalBlock obj)
        {
            ModLogger.Instance.Log(ClassName, "Parsing text into RawData");

            // Initialize the parsed data dictionary and edited lines list
            var parsedData = new Dictionary<string, ModTuple>();
            var editedLines = new List<string>();

            // Split the custom data into lines
            var lines = obj.CustomData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim())
                    .ToArray();

                if (parts.Length < 1)
                {
                    // Log error and skip if the line doesn't contain enough parts
                    ModLogger.Instance.LogError(ClassName, $"Skipping invalid line: {line}");
                    continue;
                }

                var itemDisplayName = parts[0];

                // Validate that the item display name exists in the storage
                MyDefinitionId definitionId;
                if (!_itemDefinitionStorage.ContainsKey(itemDisplayName, out definitionId))
                {
                    ModLogger.Instance.LogError(ClassName,
                        $"Item display name '{itemDisplayName}' not found in storage.");
                    continue;
                }

                // If the item is already processed, skip to avoid duplicates
                if (parsedData.ContainsKey(itemDisplayName))
                {
                    ModLogger.Instance.LogWarning(ClassName,
                        $"Duplicate entry for '{itemDisplayName}' found. Skipping.");
                    continue;
                }

                // Initialize default values
                var maxAmount = DefaultAmount;
                var percentageAboveToStartCleanup = DefaultTolerance;

                if (parts.Length == 3)
                {
                    // Try to parse the max amount, log and skip if failed
                    if (!int.TryParse(parts[1], out maxAmount))
                    {
                        ModLogger.Instance.LogError(ClassName,
                            $"Failed to parse max amount for '{itemDisplayName}' in line: {line}");
                        maxAmount = DefaultAmount;
                    }

                    // Try to parse the percentage, log and skip if failed
                    if (!float.TryParse(parts[2].TrimEnd('%'), out percentageAboveToStartCleanup))
                    {
                        ModLogger.Instance.LogError(ClassName,
                            $"Failed to parse percentage for '{itemDisplayName}' in line: {line}");
                        percentageAboveToStartCleanup = DefaultTolerance;
                    }
                }

                // Add the parsed data to the dictionary
                parsedData[itemDisplayName] = new ModTuple(maxAmount, percentageAboveToStartCleanup);

                // Format the line for CustomData
                editedLines.Add($"{itemDisplayName} | {maxAmount} | {percentageAboveToStartCleanup}%");
            }

            // Update the CustomData with the newly formatted lines
            obj.CustomData = string.Join("\n", editedLines);

            return parsedData;
        }

        private void TrackedValuesRemoveItems(IMyConveyorSorter sorter)
        {
            // Check if the ModAccessStatic instance is initialized
            var access = ModAccessStatic.Instance;
            if (access == null)
            {
                ModLogger.Instance.LogError("TrackedValuesRemoveItems", "ModAccessStatic.Instance is null.");
                return;
            }

            // Check if the ItemLimitsStorage is initialized
            var itemLimitsStorage = access.ItemLimitsStorage;
            if (itemLimitsStorage == null)
            {
                ModLogger.Instance.LogError("ClassName", "ItemLimitsStorage is null.");
            }

            // Check if MyItemLimitsCounts is initialized and contains the sorter
            Dictionary<MyDefinitionId, ModTuple> count;
            if (itemLimitsStorage?.MyItemLimitsCounts == null ||
                !itemLimitsStorage.MyItemLimitsCounts.TryGetValue(sorter, out count) ||
                count == null || count.Count <= 0)
            {
                return;
            }

            // Update the DictionaryTrackedValues
            foreach (var item in count.Where(item => itemLimitsStorage.DictionaryTrackedValues.ContainsKey(item.Key)))
            {
                itemLimitsStorage.DictionaryTrackedValues[item.Key] -= 1;
            }
        }


        private void RegisterTerminal(IMyTerminalBlock terminal)
        {
            if (terminal == null)
            {
                ModLogger.Instance.LogError(ClassName, "No idea how this specific sorter has no terminal");
                return;
            }

            if (_subscribedTerminals.Contains(terminal)) return;

            CustomData accessData;
            if (!_customDataStorage.TryGetValue(terminal, out accessData))
            {
                // Create a new CustomData instance and add it to the dictionary
                accessData = new CustomData(terminal.CustomData, terminal);
                _customDataStorage[terminal] = accessData;

                // Subscribe to the CustomDataChanged event and mark terminal as subscribed
                accessData.CustomDataChanged += Terminal_CustomDataChanged;
                _subscribedTerminals.Add(terminal);
            }
            else
            {
                // Update the existing CustomData with the new string
                accessData.CustomDataString = terminal.CustomData;

                // Even though the terminal wasn't in _subscribedTerminals, it was in _customDataStorage, so we should add it to _subscribedTerminals
                _subscribedTerminals.Add(terminal);
            }

            ModLogger.Instance.Log(ClassName, "Trash Sorter Terminal Registered");
        }

        private void UnRegisterTerminal(IMyTerminalBlock terminal)
        {
            if (terminal == null)
            {
                ModLogger.Instance.LogError(ClassName, "No idea how this specific sorter has no terminal");
                return;
            }

            if (!_subscribedTerminals.Contains(terminal)) return;

            // Log the unregistration
            ModLogger.Instance.Log(ClassName, "Trash Sorter Terminal Unregistered");

            // Unsubscribe from the CustomDataChanged event if it was subscribed
            CustomData accessData;
            if (_customDataStorage.TryGetValue(terminal, out accessData))
            {
                accessData.CustomDataChanged -= Terminal_CustomDataChanged;
            }

            // Remove the terminal from the subscribed terminals set
            _subscribedTerminals.Remove(terminal);
        }


        private void Block_OnClosing(VRage.ModAPI.IMyEntity obj)
        {
            Remove(obj as IMyConveyorSorter);
            UnRegisterTerminal(obj as IMyTerminalBlock);
            TrackedValuesRemoveItems(obj as IMyConveyorSorter);
        }

        public override void Dispose()
        {
            foreach (var terminal in _subscribedTerminals)
            {
                UnRegisterTerminal(terminal);
            }

            TrashSorters.Clear();
            HeartBeat100 -= TrashSorterStorage_HeartBeat100;
        }
    }
}