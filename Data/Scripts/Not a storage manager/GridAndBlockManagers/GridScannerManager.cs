using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Library.Collections;
using VRage.ModAPI;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.GridAndBlockManagers
{
    public class GridScannerManager : ModBase, IDisposable
    {
        private event Action<List<IMyCubeGrid>> GridListChanged;
        private readonly HashSet<IMyCubeGrid> _subscribedGrids = new HashSet<IMyCubeGrid>();
        private readonly HashSet<IMyCubeBlock> _cubeBlockWithInventory = new HashSet<IMyCubeBlock>();

        private readonly HashSet<IMyCubeBlock> _subscribedTerminals = new HashSet<IMyCubeBlock>();
        private readonly HashSet<IMyCubeBlock> _trashBlocks = new HashSet<IMyCubeBlock>();

        private readonly HashSet<IMyConveyorSorter> _myConveyorSorter = new HashSet<IMyConveyorSorter>();


        private List<IMyCubeGrid> _cubeGrids;


        private IMyCubeGrid _grid;


        public GridScannerManager(IMyEntity entity)
        {
            var block = (IMyCubeBlock)entity;
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


        private void ScanGridsForInventories()
        {
            if (GlobalStorageInstance.Instance.MyInventories.AllInventories.Count > 0)
            {
                GlobalStorageInstance.Instance.MyInventories.Dispose();
                _cubeBlockWithInventory.Clear();
            }

            _grid.GetGridGroup(GridLinkTypeEnum.Mechanical).GetGrids(_cubeGrids);

            foreach (var myGrid in _cubeGrids)
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
                    AddInventoryBlocks(myCubeBlock);
                }
            }
        }

        private void AddInventoryBlocks(IMyCubeBlock myCubeBlock)
        {
            // If no inventories return
            var inventoryCount = myCubeBlock.InventoryCount;
            if (inventoryCount <= 0) return;

            // Search for Sorters
            var iMyConveyorSorter = myCubeBlock as IMyConveyorSorter;
            if (iMyConveyorSorter != null) _myConveyorSorter.Add(iMyConveyorSorter);

            myCubeBlock.OnClosing += MyCubeBlock_OnClosing;

            // If block is names trash don't add it to the inventories
            if (Is_Trash_Designation(myCubeBlock)) return;


            for (var i = 0; i < inventoryCount; i++)
            {
                var blockInv = myCubeBlock.GetInventory(i);
                if (blockInv != null)
                {
                    GlobalStorageInstance.Instance.MyInventories.AddInventory(blockInv);
                }
            }
        }

        private bool Is_Trash_Designation(IMyCubeBlock block)
        {
            var terminal = block as IMyTerminalBlock;
            return terminal != null && Is_Trash_Designation(terminal);
        }

        private bool Is_Trash_Designation(IMyTerminalBlock terminal)
        {
            var block = (IMyCubeBlock)terminal;

            // Check if already subscribed to avoid multiple subscriptions
            if (!_subscribedTerminals.Contains(block))
            {
                terminal.CustomDataChanged += Terminal_CustomDataChanged;
                _subscribedTerminals.Add(block);
            }

            // Check the custom data after ensuring subscription
            var customData = terminal.CustomData;
            if (string.IsNullOrEmpty(customData) || !customData.Contains(TrashIdentifier))
                return false;

            // Add to trash blocks if the identifier is present
            _trashBlocks.Add(block);
            return true;
        }


        private void Terminal_CustomDataChanged(IMyTerminalBlock obj)
        {
            var block = (IMyCubeBlock)obj;

            // Check if the block is not designated as trash
            if (!Is_Trash_Designation(obj))
            {
                // If the block was previously considered trash, remove it from the list
                if (_trashBlocks.Contains(block))
                {
                    _trashBlocks.Remove(block);
                }

                // Add the block to the inventory tracking
                AddInventoryBlocks(block);
            }
            else
            {
                // If the block is designated as trash but isn't already in the list
                if (_trashBlocks.Contains(block)) return;

                // Remove all inventories associated with the block
                for (var i = 0; i < obj.InventoryCount; i++)
                {
                    GlobalStorageInstance.Instance.MyInventories.RemoveInventory(obj.GetInventory(i));
                }

                // Add the block to the trash list
                _trashBlocks.Add(block);
            }
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

        private void MyGrid_OnFatBlockAdded(MyCubeBlock myCubeBlock)
        {
            var inventoryCount = myCubeBlock.InventoryCount;
            if (inventoryCount < 0) return;
            myCubeBlock.OnClosing += MyCubeBlock_OnClosing;
            _cubeBlockWithInventory.Add(myCubeBlock);
            if (Is_Trash_Designation(myCubeBlock)) return;

            for (var i = 0; i < inventoryCount; i++)
            {
                var blockInv = myCubeBlock.GetInventory(i);
                if (blockInv != null)
                {
                    GlobalStorageInstance.Instance.MyInventories.AddInventory(blockInv);
                }
            }
        }

        private void Grid_OnClosing(IMyEntity obj)
        {
            _grid.OnGridMerge -= Grid_OnGridMerge;
            _grid.OnGridSplit -= Grid_OnGridSplit;
            _grid.OnClosing -= Grid_OnClosing;
        }

        private void UnsubscribeTerminalBlock(IMyCubeBlock cubeBlock)
        {
            var terminal = cubeBlock as IMyTerminalBlock;
            if (terminal != null) terminal.CustomDataChanged -= Terminal_CustomDataChanged;
        }

        private void MyCubeBlock_OnClosing(IMyEntity cube)
        {
            try
            {
                cube.OnClosing -= MyCubeBlock_OnClosing;
                var cubeBlock = (MyCubeBlock)cube;
                if (_subscribedTerminals.Contains(cubeBlock)) UnsubscribeTerminalBlock(cubeBlock);
                if (_trashBlocks.Contains(cubeBlock)) _trashBlocks.Remove(cubeBlock);
                _cubeBlockWithInventory.Remove(cubeBlock);
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
                    _cubeBlockWithInventory.Remove(cube);
                    if (_subscribedTerminals.Contains(cube)) UnsubscribeTerminalBlock(cube);
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

            _subscribedGrids.Clear();
            _cubeGrids.Clear();
            _cubeBlockWithInventory.Clear();
            _trashBlocks.Clear();
        }
    }
}