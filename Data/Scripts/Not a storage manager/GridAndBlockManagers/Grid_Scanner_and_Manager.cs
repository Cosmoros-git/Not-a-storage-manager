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
using VRage.Library.Collections;
using VRage.ModAPI;
using IMyConveyorSorter = Sandbox.ModAPI.IMyConveyorSorter;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.GridAndBlockManagers
{
    public class GridScanner : ModBase
    {

        private readonly HashSet<IMyCubeGrid> _subscribedGrids = new HashSet<IMyCubeGrid>();
        private readonly TrashSorterStorage _trashSorterStorage = ModAccessStatic.Instance.TrashSorterStorage;

        public readonly List<IMyCubeGrid> CubeGrids = new List<IMyCubeGrid>();
        private IMyCubeGrid _grid;

        public bool HasGlobalScanFinished;
        private readonly InventoryTerminalManager _inventoryBlocksManager;

        public GridScanner(IMyEntity entity)
        {
            ModAccessStatic.Instance.InventoryScanner = new InventoryScanner();
            var block = (IMyCubeBlock)entity;
            _grid = block.CubeGrid;

            _grid.OnGridMerge += Grid_OnGridMerge;
            _grid.OnGridSplit += Grid_OnGridSplit;

            if (!_subscribedGrids.Contains(_grid))
            {
                _grid.OnClosing += Grid_OnClosing;
                _subscribedGrids.Add(_grid);
            }

            MyAPIGateway.Utilities.ShowMessage(ClassName, $"Scanning grid for inventories");
            Scan_Grids_For_Blocks_With_Inventories();
            var sorterFilterManager = new SorterFilterManager();
            _inventoryBlocksManager = new InventoryTerminalManager();
        }

        private void Scan_Grids_For_Blocks_With_Inventories()
        {
            try
            {
                if (ModAccessStatic.Instance.InventoryScanner.AllInventories.Count > 0)
                {
                    ModAccessStatic.Instance.InventoryScanner.Dispose();
                }

                _grid.GetGridGroup(GridLinkTypeEnum.Mechanical).GetGrids(CubeGrids);

                foreach (var myGrid in CubeGrids)
                {
                    if (!_subscribedGrids.Contains(myGrid))
                    {
                        var myCubeGrid = (MyCubeGrid)_grid;
                        myCubeGrid.OnFatBlockAdded += MyGrid_OnFatBlockAdded;
                        myGrid.OnClosing += MyGrid_OnClosing;
                        _subscribedGrids.Add(myGrid);
                    }

                    var cubes = myGrid.GetFatBlocks<IMyCubeBlock>();

                    foreach (var myCubeBlock in cubes)
                    {
                        _inventoryBlocksManager.Select_Blocks_With_Inventory(myCubeBlock);
                    }
                }

                HasGlobalScanFinished = true;
                _trashSorterStorage.ForceUpdateAllSorters();
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, $"Congrats, all inventories scan fucked up {ex}");
            }
        }

       
        private void MyGrid_OnFatBlockAdded(MyCubeBlock myCubeBlock)
        {
            var inventoryCount = myCubeBlock.InventoryCount;
            if (inventoryCount < 0) return;
            MyAPIGateway.Utilities.ShowMessage(ClassName,
                $"Adding an inventory block");
            myCubeBlock.OnClosing += MyCubeBlock_OnClosing;


            if (_inventoryBlocksManager.Is_This_Trash_Block(myCubeBlock)) return;

           _inventoryBlocksManager.Add_Inventories_To_Storage(inventoryCount, myCubeBlock);
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
            var myCubeGrid = (MyCubeGrid)obj;
            myCubeGrid.OnFatBlockAdded -= MyGrid_OnFatBlockAdded;
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