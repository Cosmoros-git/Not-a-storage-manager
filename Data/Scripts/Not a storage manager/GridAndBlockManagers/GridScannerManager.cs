using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.DataClasses;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Library.Collections;
using VRage.ModAPI;
using IMyConveyorSorter = Sandbox.ModAPI.IMyConveyorSorter;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.GridAndBlockManagers
{
    public class GridScannerManager : ModBase
    {
        private event Action<IMyConveyorSorter> ModSorterAdded;


        private readonly HashSet<IMyCubeGrid> _subscribedGrids = new HashSet<IMyCubeGrid>();
        private readonly HashSet<IMyCubeBlock> _cubeBlockWithInventory = new HashSet<IMyCubeBlock>();

        private readonly HashSet<IMyCubeBlock> _subscribedTerminals = new HashSet<IMyCubeBlock>();
        private readonly HashSet<IMyCubeBlock> _trashBlocks = new HashSet<IMyCubeBlock>();

        private readonly HashSet<IMyConveyorSorter> _hashSetTrashConveyorSorter = new HashSet<IMyConveyorSorter>();
        private static readonly string[] SubtypeIdName = { "LargeTrashSorter", "SmallTrashSorter" };

        public readonly List<IMyCubeGrid> CubeGrids = new List<IMyCubeGrid>();
        private IMyCubeGrid _grid;
        private readonly ModSorterManager _manager;

        public bool HasGlobalScanFinished;

        public GridScannerManager(IMyEntity entity)
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
            Scan_Grids_For_Inventories();
            _manager = new ModSorterManager(_hashSetTrashConveyorSorter);
            ModSorterAdded += _manager.OnTrashSorterAdded;
        }

        private void Scan_Grids_For_Inventories()
        {
            try
            {
                if (ModAccessStatic.Instance.InventoryScanner.AllInventories.Count > 0)
                {
                    ModAccessStatic.Instance.InventoryScanner.Dispose();
                    _cubeBlockWithInventory.Clear();
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
                        Add_Inventory_Blocks(myCubeBlock);
                    }
                }

                HasGlobalScanFinished = true;
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, $"Congrats, all inventories scan fucked up {ex}");
            }
        }

        private void Add_Inventory_Blocks(IMyCubeBlock myCubeBlock)
        {
            // If no inventories return
            var inventoryCount = myCubeBlock.InventoryCount;
            if (inventoryCount <= 0) return;

            // Search for Sorters
            var iMyConveyorSorter = myCubeBlock as IMyConveyorSorter;
            if (iMyConveyorSorter != null) _hashSetTrashConveyorSorter.Add(iMyConveyorSorter);

            myCubeBlock.OnClosing += MyCubeBlock_OnClosing;

            // If block is names trash don't add it to the inventories
            if (Is_Trash_Designation_Function(myCubeBlock)) return;

            Add_Inventories_To_Storage(inventoryCount, myCubeBlock);
        }

        private static void Add_Inventories_To_Storage(int inventoryCount, IMyCubeBlock block)
        {
            for (var i = 0; i < inventoryCount; i++)
            {
                var blockInv = block.GetInventory(i);
                if (blockInv != null)
                {
                    ModAccessStatic.Instance.InventoryScanner.AddInventory(blockInv);
                }
            }
        }

        private static void Remove_Inventories_From_Storage(int inventoryCount, IMyCubeBlock block)
        {
            for (var i = 0; i < inventoryCount; i++)
            {
                var blockInv = block.GetInventory(i);
                if (blockInv != null)
                {
                    ModAccessStatic.Instance.InventoryScanner.RemoveInventory((MyInventory)blockInv);
                }
            }
        }


        // Checks block Custom data for if it has [TRASH] tag.
        private bool Is_Trash_Designation_Function(IMyCubeBlock block)
        {
            var terminal = block as IMyTerminalBlock;
            return terminal != null && Is_Trash_Designation_Function(terminal);
        }

        private bool Is_Trash_Designation_Function(IMyTerminalBlock terminal)
        {
            var block = (IMyCubeBlock)terminal;

            // Check if already subscribed to avoid multiple subscriptions
            if (!_subscribedTerminals.Contains(block))
            {
                if (!Is_Conveyor_Sorter(block))
                {
                    terminal.CustomDataChanged += Terminal_CustomDataChanged;
                    _subscribedTerminals.Add(block);
                }
                else
                {
                    var subtypeId = block.BlockDefinition.SubtypeId;
                    if (SubtypeIdName.Contains(subtypeId))
                    {
                        var sorter = block as IMyConveyorSorter;
                        if (!_hashSetTrashConveyorSorter.Contains(sorter))
                        {
                            _hashSetTrashConveyorSorter.Add(sorter);
                            ModSorterAdded?.Invoke(sorter);
                        }
                    }
                }
            }

            // Check the custom data after ensuring subscription
            var customData = terminal.CustomData;
            if (string.IsNullOrEmpty(customData) || !customData.Contains(TrashIdentifier))
                return false;

            // Add to trash blocks if the identifier is present
            _trashBlocks.Add(block);
            return true;
        }


        private static bool Is_Conveyor_Sorter(IMyCubeBlock block)
        {
            var isConveyor = block as IMyConveyorSorter;
            return isConveyor != null;
        }


        private void Terminal_CustomDataChanged(IMyTerminalBlock obj)
        {
            var block = (IMyCubeBlock)obj;

            // Check if the block is not designated as trash
            if (!Is_Trash_Designation_Function(obj))
            {
                // If the block was previously considered trash, remove it from the list
                if (_trashBlocks.Contains(block))
                {
                    _trashBlocks.Remove(block);
                }

                // Add the block to the inventory tracking
                Add_Inventory_Blocks(block);
            }
            else
            {
                // If the block is designated as trash but isn't already in the list
                if (_trashBlocks.Contains(block)) return;

                // Remove all inventories associated with the block
                Remove_Inventories_From_Storage(obj.InventoryCount, block);

                // Add the block to the trash list
                _trashBlocks.Add(block);
            }
        }

        private void MyGrid_OnFatBlockAdded(MyCubeBlock myCubeBlock)
        {
            var inventoryCount = myCubeBlock.InventoryCount;
            if (inventoryCount < 0) return;
            MyAPIGateway.Utilities.ShowMessage(ClassName,
                $"Adding an inventory block");
            myCubeBlock.OnClosing += MyCubeBlock_OnClosing;
            _cubeBlockWithInventory.Add(myCubeBlock);
            Is_Conveyor_Sorter(myCubeBlock);


            if (Is_Trash_Designation_Function(myCubeBlock)) return;

            Add_Inventories_To_Storage(inventoryCount, myCubeBlock);
        }


        private void Grid_OnGridSplit(IMyCubeGrid arg1, IMyCubeGrid arg2)
        {
            MyAPIGateway.Utilities.ShowMessage(ClassName,
                $"Grid_OnSplit happend");
            Scan_Grids_For_Inventories();
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
            Scan_Grids_For_Inventories();
        }


        private void Grid_OnClosing(IMyEntity obj)
        {
            _grid.OnGridMerge -= Grid_OnGridMerge;
            _grid.OnGridSplit -= Grid_OnGridSplit;
            _grid.OnClosing -= Grid_OnClosing;
        }

        private void Unsubscribe_Terminal_Block(IMyCubeBlock cubeBlock)
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
                if (_subscribedTerminals.Contains(cubeBlock)) Unsubscribe_Terminal_Block(cubeBlock);
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
                    ModAccessStatic.Instance.InventoryScanner.RemoveInventory((MyInventory)blockInv);
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
                    if (_subscribedTerminals.Contains(cube)) Unsubscribe_Terminal_Block(cube);
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
                        ModAccessStatic.Instance.InventoryScanner.RemoveInventory((MyInventory)blockInv);
                    }
                }
            }
        }

        public override void Dispose()
        {
            MyAPIGateway.Utilities.ShowMessage(ClassName, "OnDispose was called");
            try
            {
                // Safeguard: Check if _cubeGrids is not null before iterating
                if (CubeGrids != null)
                {
                    foreach (var cubeGrid in CubeGrids)
                    {
                        try
                        {
                            if (cubeGrid != null)
                            {
                                MyGrid_OnClosing(cubeGrid);
                            }
                        }
                        catch (Exception ex)
                        {
                            MyAPIGateway.Utilities.ShowMessage(ClassName,
                                $"Error closing grid {cubeGrid?.DisplayName}: {ex}");
                        }
                    }
                }

                // Safeguard: Unsubscribe from events
                try
                {
                    if (_manager != null) ModSorterAdded -= _manager.OnTrashSorterAdded;
                }
                catch (Exception ex)
                {
                    MyAPIGateway.Utilities.ShowMessage(ClassName, $"Dispose, error on event unsubscription: {ex}");
                }
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, $"Dispose, error during cleanup: {ex}");
            }
            finally
            {
                // Safeguard: Clear collections if they are not null
                _subscribedGrids?.Clear();
                CubeGrids?.Clear();
                _cubeBlockWithInventory?.Clear();
                _trashBlocks?.Clear();
                _hashSetTrashConveyorSorter?.Clear();
                _subscribedTerminals?.Clear();
            }
        }
    }
}