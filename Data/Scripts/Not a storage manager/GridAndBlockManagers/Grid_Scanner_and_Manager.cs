using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.DataClasses;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StorageSubclasses;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.GridAndBlockManagers
{
    public class GridScanner : ModBase
    {
        public readonly HashSet<IMyCubeGrid> CubeGrids = new HashSet<IMyCubeGrid>();
        private readonly HashSet<IMyCubeGrid> _subscribedGrids = new HashSet<IMyCubeGrid>();
        private readonly TrashSorterStorage _trashSorterStorage;
        private readonly InventoryTerminalManager _inventoryBlocksManager;
        private readonly ModLogger _modLogger = ModAccessStatic.Instance.Logger;


        private IMyCubeGrid _grid;

        private bool _isItNotAFirstScan;
        public bool HasGlobalScanFinished;

        public GridScanner(IMyCubeBlock entity, TrashSorterStorage trashSorterStorage, InventoryTerminalManager inventoryTerminalManager)
        {
            _grid = entity.CubeGrid;
            _trashSorterStorage = trashSorterStorage;
            _inventoryBlocksManager = inventoryTerminalManager;

            _grid.OnGridMerge += Grid_OnGridMerge;
            _grid.OnGridSplit += Grid_OnGridSplit;

            _modLogger.Log(ClassName, $"Scanning grid for inventories");
            Scan_Grids_For_Blocks_With_Inventories();
        }

        private void Scan_Grids_For_Blocks_With_Inventories()
        {
            try
            {
                // Check if InventoryScanner exists and AllInventories is not null
                if (ModAccessStatic.Instance?.InventoryScanner?.AllInventories != null &&
                    ModAccessStatic.Instance.InventoryScanner.AllInventories.Count > 0 && _isItNotAFirstScan)
                {
                    ModAccessStatic.Instance.InventoryScanner.Dispose();
                }

                _isItNotAFirstScan = true;

                // Ensure _grid is not null before getting grid group
                if (_grid == null)
                {
                    _modLogger.LogError(ClassName, "Grid is null.");
                    return;
                }

                _grid.GetGridGroup(GridLinkTypeEnum.Mechanical)?.GetGrids(CubeGrids);

                // Ensure CubeGrids is initialized and has grids to process
                if (CubeGrids == null || CubeGrids.Count == 0)
                {
                    _modLogger.LogError(ClassName, "No grids found in CubeGrids.");
                    return;
                }

                foreach (var myGrid in CubeGrids)
                {
                    if (!_subscribedGrids.Contains(myGrid))
                    {
                        if (myGrid != null)
                        {
                            _modLogger.Log(ClassName, $"Subbing to.{myGrid.CustomName}");
                            var grid = (MyCubeGrid)myGrid;
                            grid.OnFatBlockAdded += MyGrid_OnFatBlockAdded;
                            myGrid.OnClosing += MyGrid_OnClosing;
                            _subscribedGrids.Add(myGrid);
                        }
                        else
                        {
                            _modLogger.LogError(ClassName, "Failed to cast grid to MyCubeGrid.");
                            return;
                        }
                    }

                    var cubes = myGrid?.GetFatBlocks<IMyCubeBlock>();

                    // Ensure cubes is not null before processing
                    if (cubes == null)
                    {
                        _modLogger.LogWarning(ClassName, "No fat blocks found in grid.");
                        continue;
                    }

                    foreach (var myCubeBlock in cubes)
                    {
                        if (_inventoryBlocksManager != null)
                        {
                            _inventoryBlocksManager.Select_Blocks_With_Inventory(myCubeBlock);
                        }
                        else
                        {
                            _modLogger.LogError(ClassName, "_inventoryBlocksManager is null.");
                            return;
                        }
                    }
                }

                HasGlobalScanFinished = true;
                _trashSorterStorage.ForceUpdateAllSorters();
            }
            catch (Exception ex)
            {
                _modLogger.LogError(ClassName, $"Congrats, all inventories scan messed up: {ex}");
            }
        }


        private void MyGrid_OnFatBlockAdded(MyCubeBlock fatBlock)
        {
            var inventoryCount = fatBlock.InventoryCount;
            if (inventoryCount < 0) return;

            fatBlock.OnClosing += MyCubeBlock_OnClosing;


            if (_inventoryBlocksManager.Is_This_Trash_Block(fatBlock)) return;

            _inventoryBlocksManager.Add_Inventories_To_Storage(inventoryCount, fatBlock);
        }

        // Todo optimize this
        private void Grid_OnGridSplit(IMyCubeGrid arg1, IMyCubeGrid arg2)
        {
            MyAPIGateway.Utilities.ShowMessage(ClassName,
                $"Grid_OnSplit happend");
            Scan_Grids_For_Blocks_With_Inventories();
        }

        private void Grid_OnGridMerge(IMyCubeGrid arg1, IMyCubeGrid arg2)
        {
            if (_grid == arg2)
            {
                _grid = arg1;
            }
            else
            {
                Dispose();
                return;
            }

            MyAPIGateway.Utilities.ShowMessage(ClassName,
                $"Grid_OnMerge happend");
            Scan_Grids_For_Blocks_With_Inventories();
        }


        private void Grid_OnClosing(IMyEntity obj)
        {
            _grid.OnGridMerge -= Grid_OnGridMerge;
            _grid.OnGridSplit -= Grid_OnGridSplit;
            _grid.OnClosing -= Grid_OnClosing;
        }

        private void MyCubeBlock_OnClosing(IMyEntity cube)
        {
            try
            {
                cube.OnClosing -= MyCubeBlock_OnClosing;
                var cubeBlock = (MyCubeBlock)cube;
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, $"MyCubeBlock_OnClosing, on error on un-sub {ex}");
            }

            var inventoryCount = cube.InventoryCount;
            for (var i = 0; i < inventoryCount; i++)
            {
                var blockInv = cube.GetInventory(i);
                if (blockInv != null)
                {
                    ModAccessStatic.Instance.InventoryScanner.RemoveInventory((MyInventory)blockInv);
                }
            }
        }

        private void MyGrid_OnClosing(IMyEntity obj)
        {
            
            obj.OnClosing -= MyGrid_OnClosing;
            var myCubeGrid = (IMyCubeGrid)obj;
            if (myCubeGrid == _grid)
            {
                myCubeGrid.OnGridMerge-= Grid_OnGridMerge;
                myCubeGrid.OnGridSplit-= Grid_OnGridSplit;
            }
            var grid = (MyCubeGrid)myCubeGrid;
            grid.OnFatBlockAdded -= MyGrid_OnFatBlockAdded;
            _subscribedGrids.Remove(myCubeGrid);
            var cubes = myCubeGrid.GetFatBlocks<MyCubeBlock>().Where(x => x.InventoryCount > 0);
            foreach (var cube in cubes)
            {
                try
                {
                    cube.OnClosing -= MyCubeBlock_OnClosing;
                }
                catch (Exception ex)
                {
                    MyAPIGateway.Utilities.ShowMessage(ClassName, $"MyCubeBlock_OnClosing, on error on un-sub {ex}");
                }

                var inventoryCount = cube.InventoryCount;
                for (var i = 0; i < inventoryCount; i++)
                {
                    var blockInv = cube.GetInventory(i);
                    if (blockInv != null)
                    {
                        ModAccessStatic.Instance.InventoryScanner.RemoveInventory(blockInv);
                    }
                }
            }
        }
    }
}