using System;
using System.Collections.Generic;
using System.Linq;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.GridScanner
{
    public class GridScannerManager : ModBase, IDisposable
    {
        private readonly HashSet<IMyCubeGrid> _subscribedGrids = new HashSet<IMyCubeGrid>();
        private List<IMyCubeGrid> _cubeGrids = new List<IMyCubeGrid>();

        private readonly IMyCubeBlock block;
        private IMyCubeGrid _grid;


        public GridScannerManager(IMyEntity entity)
        {
            _entity = entity;
            block = (IMyCubeBlock)entity;
            _grid = block.CubeGrid;
            _grid.OnGridMerge += Grid_OnGridMerge;
            _grid.OnGridSplit += Grid_OnGridSplit;
            if (!_subscribedGrids.Contains(_grid))
            {
                _grid.OnClosing += Grid_OnClosing;
                _subscribedGrids.Add(_grid);
            }

            ScanGridsForInventories();
        }

        private void Grid_OnGridSplit(IMyCubeGrid arg1, IMyCubeGrid arg2)
        {
            ScanGridsForInventories();
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

            ScanGridsForInventories();
        }

        private void Grid_OnClosing(IMyEntity obj)
        {
            _grid.OnGridMerge -= Grid_OnGridMerge;
            _grid.OnGridSplit -= Grid_OnGridSplit;
            _grid.OnClosing -= Grid_OnClosing;
        }

        private void ScanGridsForInventories()
        {
            if (GlobalStorageInstance.Instance.MyInventories.AllInventories.Count > 0)
            {
                GlobalStorageInstance.Instance.MyInventories.Dispose();
            }

            var cubeGrids = new List<IMyCubeGrid>();
            _grid.GetGridGroup(GridLinkTypeEnum.Mechanical).GetGrids(cubeGrids);
            _cubeGrids = cubeGrids;

            foreach (var myGrid in cubeGrids)
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
                    var inventoryCount = myCubeBlock.InventoryCount;
                    if (inventoryCount <= 0) return;
                    myCubeBlock.OnClosing += MyCubeBlock_OnClosing;
                    for (var i = 0; i < inventoryCount; i++)
                    {
                        var blockInv = myCubeBlock.GetInventory(i);
                        if (blockInv != null)
                        {
                            GlobalStorageInstance.Instance.MyInventories.AddInventory(blockInv);
                        }
                    }
                }
            }
        }

        private void MyGrid_OnFatBlockAdded(MyCubeBlock myCubeBlock)
        {
            var inventoryCount = myCubeBlock.InventoryCount;
            if (inventoryCount < 0) return;
            myCubeBlock.OnClosing += MyCubeBlock_OnClosing;
            for (var i = 0; i < inventoryCount; i++)
            {
                var blockInv = myCubeBlock.GetInventory(i);
                if (blockInv != null)
                {
                    GlobalStorageInstance.Instance.MyInventories.AddInventory(blockInv);
                }
            }
        }

        private void MyCubeBlock_OnClosing(IMyEntity cube)
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
                    GlobalStorageInstance.Instance.MyInventories.RemoveInventory(blockInv);
                }
            }
        }

        private void MyGrid_OnClosing(IMyEntity obj)
        {
            var myCubeBlock = (IMyCubeBlock)obj;
            var myGrid = myCubeBlock.CubeGrid;
            myGrid.OnClosing -= MyGrid_OnClosing;
            var myCubeGrid = (MyCubeGrid)myGrid;
            myCubeGrid.OnFatBlockAdded -= MyGrid_OnFatBlockAdded;
            _subscribedGrids.Remove(myGrid);
            var cubes = myGrid.GetFatBlocks<IMyCubeBlock>().Where(x => x.InventoryCount > 0);
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
                        GlobalStorageInstance.Instance.MyInventories.RemoveInventory(blockInv);
                    }
                }
            }
        }

        public void Dispose()
        {
            MyAPIGateway.Utilities.ShowMessage(ClassName, "OnDispose was called");
            try
            {
                foreach (var cubeGrid in _cubeGrids)
                {
                    MyGrid_OnClosing(cubeGrid);
                }
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, $"Dispose, error on un-sub {ex}");
            }

            _cubeGrids.Clear();
        }
    }
}