using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.GridAndBlockManagers;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using Sandbox.Game.EntityComponents;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager
{
    public class ModInitializer : ModBase
    {
        private int _loadingStep = 1;

        private readonly IMyEntity _entity;
        private readonly IMyCubeBlock _varImyCubeBlock;
        private readonly IMyCubeGrid _varIMyCubeGrid;
        private readonly MyCubeBlock _varImyCube;

        public ModAccessStatic GlobalStorageInstance;
        public GridScannerManager VarGridScannerManager;
        public bool IsThereGridManagerOverlap;
        private bool _hasBeenMarkedMessage;


        public ModInitializer(IMyEntity entity)
        {
            _entity = entity;
            _varImyCubeBlock = (IMyCubeBlock)entity;
            _varIMyCubeGrid = _varImyCubeBlock.CubeGrid;
            _varImyCube = (MyCubeBlock)entity;
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
            if(!_hasBeenMarkedMessage) MyAPIGateway.Utilities.ShowMessage(ClassName, $"Marking grid as managed by this block");
            if (grid.Storage == null)
            {
                grid.Storage = new MyModStorageComponent();
            }

            grid.Storage.SetValue(ManagedKey, _varImyCubeBlock.EntityId.ToString());
            _hasBeenMarkedMessage= true;
        }


        public bool VerifyLaunch()
        {
            if (!IsThereGridManagerOverlap)
            {
                if (_varImyCubeBlock == null || _varIMyCubeGrid == null)
                {
                    MyAPIGateway.Utilities.ShowMessage(ClassName, "Block or Grid is null.");
                    return false;
                }

                if (_varIMyCubeGrid.Physics == null)
                {
                    MyAPIGateway.Utilities.ShowMessage(ClassName, "Grid has no physics. Retrying in the next frame...");
                    return false;
                }

                if (_varIMyCubeGrid.Storage == null)
                {
                    MarkGridAsManaged(_varIMyCubeGrid);
                    IsThereGridManagerOverlap = false;
                }
                else if (!IsGridManagedByThisBlock(_varIMyCubeGrid))
                {
                    MyAPIGateway.Utilities.ShowMessage(ClassName, "Grid manager overlap");
                    IsThereGridManagerOverlap = true;
                    return false;
                }
            }
            else if (_varIMyCubeGrid.Storage == null || !IsGridManagedByThisBlock(_varIMyCubeGrid))
            {
                if (!IsThereGridManagerOverlap)
                    MyAPIGateway.Utilities.ShowMessage(ClassName, $"There is manager block overlap");
                IsThereGridManagerOverlap = true;

                return false;
            }
            else
            {
                IsThereGridManagerOverlap = false;
            }


            MyAPIGateway.Utilities.ShowMessage(ClassName,
                $"Grid: {_varIMyCubeGrid.DisplayName}, OwnerId: {_varImyCubeBlock.OwnerId}, Faction Tag: {_varImyCubeBlock.GetOwnerFactionTag()}");
            _managedGrids.Add(_varIMyCubeGrid);
            HeartBeat10 += LoadingSequence;
            return true;
        }

        public void LoadingSequence()
        {
            switch (_loadingStep)
            {
                case 1:
                    // This step initializes reference tables and access to them for other class.
                    // Classes at play: GetDefinitions, CreateReferenceTables, ModAccessStatic

                    GlobalStorageInstance = new ModAccessStatic();
                    MyAPIGateway.Utilities.ShowMessage(ClassName,
                        $"Loading sequence step 1");
                    _loadingStep++;
                    break;


                case 2:

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
                        VarGridScannerManager = new GridScannerManager(_entity);
                        MyAPIGateway.Utilities.ShowMessage(ClassName,
                            $"Loading sequence step 2");
                    }


                    break;
                case 3:
                    if (VarGridScannerManager != null && ModAccessStatic.Instance.InventoryScanner != null)
                    {
                        if (!VarGridScannerManager.HasGlobalScanFinished) return;
                        ModAccessStatic.Instance.InventoryScanner.ScanAllInventories();
                        _loadingStep++;
                        VarGridScannerManager = new GridScannerManager(_entity);
                        _managedGrids.UnionWith(VarGridScannerManager.CubeGrids);
                        MyAPIGateway.Utilities.ShowMessage(ClassName,
                            $"Loading sequence step 3");
                    }

                    break;


                case 4:
                    if (_managedGrids != null)
                        foreach (var grid in _managedGrids)
                        {
                            MarkGridAsManaged(grid);
                        }

                    MyAPIGateway.Utilities.ShowMessage(ClassName,
                        $"Loading sequence step 4");
                    _loadingStep++;
                    break;
                case 5:
                    break;
                case 6:
                    break;
            }
        }

        private void RemoveManager(IMyCubeGrid grid)
        {
            if (grid.Storage == null) return;
            if (!grid.Storage.ContainsKey(ManagedKey)) return;
            // Log or notify that the grid is no longer managed
            MyAPIGateway.Utilities.ShowMessage(ClassName, $"Removing manager for grid: {grid.DisplayName}");

            // Remove the managed key
            grid.Storage.Clear();
        }


        public override void Dispose()
        {
            if (!IsGridManagedByThisBlock(_varIMyCubeGrid)) return;
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