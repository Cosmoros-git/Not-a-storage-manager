using System;
using System.Collections.Generic;
using System.Linq;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.DataClasses;
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
    internal class SorterFilterManager : ModBase
    {
        private readonly TrashSorterStorage _myTrashSorterStorage;


        public Dictionary<IMyConveyorSorter, Dictionary<MyDefinitionId, ModTuple>> MyItemLimitsCounts =
            new Dictionary<IMyConveyorSorter, Dictionary<MyDefinitionId, ModTuple>>();

        public Dictionary<MyDefinitionId, int> DictionaryTrackedValues = new Dictionary<MyDefinitionId, int>();

        public static Dictionary<IMyConveyorSorter, List<MyInventoryItemFilter>> FilterSorters =
            new Dictionary<IMyConveyorSorter, List<MyInventoryItemFilter>>();

        private readonly HashSet<MyDefinitionId> _changedDefinitions = new HashSet<MyDefinitionId>();
        private readonly ItemDefinitionStorage _itemDefinitionStorage;
        private readonly ModLogger _modLogger = ModAccessStatic.Instance.Logger;


        public SorterFilterManager(TrashSorterStorage trashSorterStorage, ItemDefinitionStorage itemDefinitionStorage)
        {
            _myTrashSorterStorage = trashSorterStorage;
            _itemDefinitionStorage = itemDefinitionStorage;
            ResetConveyorsFilters();

            _itemDefinitionStorage.ValueChanged += OnValueChanged;
            HeartBeat100 += HeartbeatInstance_HeartBeat100;
        }
        private void ResetConveyorsFilters()
        {
            foreach (var sorter in _myTrashSorterStorage.TrashSorters)
            {
                if (!sorter.CustomData.Contains("[TRASH COLLECTOR]")) continue;
                var emptyFilter = new List<MyInventoryItemFilter>();
                sorter.SetFilter(MyConveyorSorterMode.Whitelist, emptyFilter);
            }

            FilterSorters.Clear();
        }
        private void HeartbeatInstance_HeartBeat100()
        {
            ProcessChanges();
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
                    if (!_itemDefinitionStorage.TryGetValue(definitionId, out value)) continue;

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
                                    RemoveFromConveyorSorterFilter(sorter, definitionId);
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
                            _modLogger.LogError(ClassName,
                                $"Unexpected state for {definitionId} in ProcessChanges.");
                            break;
                    }
                }
            }

            // Clear the set after processing
            _changedDefinitions.Clear();
        }


        // Function to add and remove items from filters.
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
                _modLogger.LogError(ClassName, $"Failed to add to filter: {ex.Message}");
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
                _modLogger.LogError(ClassName, $"Failed to remove a filter: {ex.Message}");
            }
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


        public override void Dispose()
        {
            // Safeguard: Check if _itemStorage is not null before unsubscribing from ValueChanged event
            if (_itemDefinitionStorage == null) return;
            try
            {
                _itemDefinitionStorage.ValueChanged -= OnValueChanged;
            }
            catch (Exception ex)
            {
                _modLogger.LogError(ClassName,
                    $"Error unsubscribing from ValueChanged event: {ex}");
            }
        }
    }
}