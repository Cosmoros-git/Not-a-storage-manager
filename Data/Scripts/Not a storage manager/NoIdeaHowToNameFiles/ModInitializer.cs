using System;
using System.Collections.Generic;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.DataClasses;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.GridAndBlockManagers;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StorageSubclasses;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.NoIdeaHowToNameFiles
{
    public class ModInitializer : ModBase
    {
        private int _loadingStep = 1;

        private readonly IMyEntity _entity;
        private readonly IMyCubeBlock _varImyCubeBlock;
        private readonly IMyCubeGrid _varIMyCubeGrid;

        public ModAccessStatic GlobalStorageInstance;
        public GridScanner VarGridScannerManager;
        public bool IsThereGridManagerOverlap;
        private bool _hasBeenMarkedMessage;


        public ModInitializer(IMyEntity entity)
        {
            GlobalStorageInstance = new ModAccessStatic();
            _entity = entity;
            _varImyCubeBlock = (IMyCubeBlock)entity;
            _varIMyCubeGrid = _varImyCubeBlock.CubeGrid;
        }

        private static readonly Guid ManagedKey = new Guid("080d0d52-603a-4be5-933b-e6f9043ac22c");
        private readonly HashSet<IMyCubeGrid> _managedGrids = new HashSet<IMyCubeGrid>();

        // Check if the grid is already managed
        public bool IsGridManagedByThisBlock(IMyCubeGrid grid)
        {
            string storedValue;
            if (grid.Storage == null || !grid.Storage.TryGetValue(ManagedKey, out storedValue)) return false;
            if (!string.IsNullOrEmpty(storedValue)) return storedValue == _varImyCubeBlock.EntityId.ToString();
            MarkGridAsManaged(_varIMyCubeGrid);
            return true;
        }

        // Mark the grid as managed
        public void MarkGridAsManaged(IMyCubeGrid grid)
        {
            if (!_hasBeenMarkedMessage)
                ModLogger.Instance.Log(ClassName, "Marking grid as managed by this block");
            if (grid.Storage == null)
            {
                grid.Storage = new MyModStorageComponent();
            }

            grid.Storage.SetValue(ManagedKey, _varImyCubeBlock.EntityId.ToString());
            _hasBeenMarkedMessage = true;
        }


        public bool VerifyLaunch()
        {
            try
            {
                if (ModLogger.Instance == null)
                {
                    MyAPIGateway.Utilities.ShowMessage(ClassName, "ModLogger is fucking null peace of shit");
                    return false;
                }

                ModLogger.Instance.ManagingBlockId = _entity.EntityId.ToString();
                if (!IsThereGridManagerOverlap)
                {
                    if (_varImyCubeBlock == null || _varIMyCubeGrid == null)
                    {
                        ModLogger.Instance.LogError(ClassName, "Block or Grid is null.");
                        return false;
                    }

                    if (_varIMyCubeGrid.Physics == null)
                    {
                        ModLogger.Instance.LogError(ClassName, "Grid has no physics. Retrying in the next frame...");
                        return false;
                    }

                    if (_varIMyCubeGrid.Storage == null)
                    {
                        MarkGridAsManaged(_varIMyCubeGrid);
                        IsThereGridManagerOverlap = false;
                    }
                    else if (!IsGridManagedByThisBlock(_varIMyCubeGrid))
                    {
                        ModLogger.Instance.LogError(ClassName, "Grid manager overlap");
                        IsThereGridManagerOverlap = true;
                        return false;
                    }
                }
                else if (_varIMyCubeGrid.Storage == null || !IsGridManagedByThisBlock(_varIMyCubeGrid))
                {
                    if (!IsThereGridManagerOverlap)
                        ModLogger.Instance.LogError(ClassName, $"There is manager block overlap");
                    IsThereGridManagerOverlap = true;

                    return false;
                }
                else
                {
                    IsThereGridManagerOverlap = false;
                }


                ModLogger.Instance.Log(ClassName,
                    $"Grid: {_varIMyCubeGrid.DisplayName}, OwnerId: {_varImyCubeBlock.OwnerId}, Faction Tag: {_varImyCubeBlock.GetOwnerFactionTag()}");
                _managedGrids.Add(_varIMyCubeGrid);
                HeartBeat10 += LoadingSequence;
                return true;
            }
            catch (Exception)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, "ModLogger is fucking null peace of shit");
                return false;
            }
        }

        public void LoadingSequence()
        {
            try
            {
                switch (_loadingStep)
                {
                    case 1:
                        // This step initializes reference tables and access to them for other class.
                        // Classes at play: GetDefinitions, CreateReferenceTables, ModAccessStatic
                        try
                        {
                            
                            var itemStorage = new ItemDefinitionStorage();
                            var itemLimitsStorage = new ItemLimitsStorage();

                            var referenceDictionaryCreator = new ReferenceDictionaryCreator(itemStorage);
                            var trashSorterStorage = new TrashSorterStorage(itemStorage, itemLimitsStorage);
                            var inventoryScanner = new InventoryScanner(itemStorage);
                            var inventoryTerminalManager = new InventoryTerminalManager(trashSorterStorage);

                            ModAccessStatic.Instance.InitiateValues(referenceDictionaryCreator, itemLimitsStorage,
                                itemStorage, inventoryScanner, trashSorterStorage, inventoryTerminalManager);
                            try
                            {
                                ModLogger.Instance.Log(ClassName,
                                    $"Loading sequence step 1");
                            }
                            catch (Exception)
                            {
                                MyAPIGateway.Utilities.ShowMessage(ClassName, "Step 1 Logger shit itself.");
                                _loadingStep = -1;
                            }

                            _loadingStep++;
                        }
                        catch (Exception)
                        {
                            MyAPIGateway.Utilities.ShowMessage(ClassName, "Step 1 shit itself.");
                            _loadingStep = -1;
                        }

                        break;


                    case 2:
                        try
                        {
                            // This initializes GridScanner. And populates inventories
                            if (VarGridScannerManager != null)
                            {
                                if (VarGridScannerManager.HasGlobalScanFinished)
                                {
                                    _loadingStep++;
                                }
                            }
                            else
                            {
                                VarGridScannerManager = new GridScanner(_entity as IMyCubeBlock,
                                    ModAccessStatic.Instance.InventoryTerminalManager);
                                ModLogger.Instance.Log(ClassName,
                                    $"Loading sequence step 2");
                            }
                        }
                        catch (Exception)
                        {
                            MyAPIGateway.Utilities.ShowMessage(ClassName, "Step 2 shit itself.");
                            _loadingStep = -1;
                        }

                        break;
                    case 3:
                        try
                        {
                            if (VarGridScannerManager != null && ModAccessStatic.Instance.InventoryScanner != null)
                            {
                                if (!VarGridScannerManager.HasGlobalScanFinished) return;
                                ModAccessStatic.Instance.InventoryScanner.ScanAllInventories();
                                _loadingStep++;
                                _managedGrids.UnionWith(VarGridScannerManager.CubeGrids);
                                ModLogger.Instance.Log(ClassName,
                                    $"Loading sequence step 3");
                            }
                        }
                        catch (Exception)
                        {
                            MyAPIGateway.Utilities.ShowMessage(ClassName, "Step 2 shit itself.");
                            _loadingStep = -1;
                        }

                        break;


                    case 4:
                        if (_managedGrids != null)
                            foreach (var grid in _managedGrids)
                            {
                                MarkGridAsManaged(grid);
                            }

                        ModLogger.Instance.Log(ClassName,
                            $"Loading sequence step 4");
                        _loadingStep++;
                        break;
                    case 5:
                        break;
                    case 6:
                        break;
                }
            }
            catch (Exception)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, "Okey wtf?");
                _loadingStep = -1;
            }
        }

        private void RemoveManager(IMyCubeGrid grid)
        {
            if (grid.Storage == null) return;
            if (!grid.Storage.ContainsKey(ManagedKey)) return;
            // Log or notify that the grid is no longer managed
            ModLogger.Instance.LogWarning(ClassName, $"Removing manager for grid: {grid.DisplayName}");

            // Remove the managed key
            grid.Storage.RemoveValue(ManagedKey);
        }


        public override void Dispose()
        {
            if (!IsGridManagedByThisBlock(_varIMyCubeGrid)) return;
            if (ModLogger.Instance == null) return;
            if (!ModLogger.Instance.IsSessionUnloading) return;
            if (_varIMyCubeGrid != null)
            {
                if (_managedGrids != null && _managedGrids.Count > 0)
                {
                    foreach (var grid in _managedGrids)
                    {
                        RemoveManager(grid);
                    }
                }
                else
                {
                    RemoveManager(_varIMyCubeGrid);
                }
            }

            MyAPIGateway.Utilities.ShowMessage(ClassName, "Removing block as manager");
        }
    }
}