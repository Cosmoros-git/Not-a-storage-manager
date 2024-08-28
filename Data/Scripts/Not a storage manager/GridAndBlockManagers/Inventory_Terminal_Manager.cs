using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.DataClasses;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageRender.Messages;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.GridAndBlockManagers
{
    internal class InventoryTerminalManager : ModBase
    {
        private readonly HashSet<IMyTerminalBlock> _subscribedTerminals = new HashSet<IMyTerminalBlock>();
        private readonly HashSet<IMyCubeBlock> _trashBlocks = new HashSet<IMyCubeBlock>();

        private readonly TrashSorterStorage _trashConveyorSorterStorage = ModAccessStatic.Instance.SortersStorage;


        public void Select_Blocks_With_Inventory(IMyCubeBlock myCubeBlock)
        {
            // If no inventories return
            var inventoryCount = myCubeBlock.InventoryCount;
            if (inventoryCount <= 0) return;

            myCubeBlock.OnClosing += MyCubeBlock_OnClosing;
            // Search for Sorters
            var iMyConveyorSorter = myCubeBlock as IMyConveyorSorter;
            if (iMyConveyorSorter != null)
                if (_trashConveyorSorterStorage.Add(iMyConveyorSorter))
                    return;
            // If block is names trash don't add it to the inventories
            SubscribeBlock(myCubeBlock);
            if (Is_This_Trash_Block(myCubeBlock)) return;
            Add_Inventories_To_Storage(inventoryCount, myCubeBlock);
            
        }


        public void Add_Inventories_To_Storage(int inventoryCount, IMyCubeBlock block)
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
        public bool Is_This_Trash_Block(IMyCubeBlock block)
        {
            var terminal = block as IMyTerminalBlock;
            return terminal != null && Is_This_Trash_Block(terminal);
        }
        public bool Is_This_Trash_Block(IMyTerminalBlock terminal)
        {
            var block = (IMyCubeBlock)terminal;
            var sorter = block as IMyConveyorSorter;
            if (_trashConveyorSorterStorage.Add(sorter))
            {
                terminal.CustomDataChanged -= Terminal_CustomDataChanged; // Removing sub to not double call.
                return true;
            }

            var customData = terminal.CustomData;
            if (string.IsNullOrEmpty(customData) || !customData.Contains(TrashIdentifier))
                return false;
            _trashBlocks.Add(block);
            return true;


        }


        private void SubscribeBlock(IMyCubeBlock block)
        {
            var terminal = (IMyTerminalBlock)block;
            if (terminal == null) return;
            if (_subscribedTerminals.Contains(terminal)) return;
            terminal.CustomDataChanged += Terminal_CustomDataChanged;
            _subscribedTerminals.Add(terminal);
        }
        public void UnsubscribeBlock(IMyCubeBlock block)
        {
            var terminal = (IMyTerminalBlock)block;
            if (terminal == null) return;
            if (!_subscribedTerminals.Contains(terminal)) return;
            terminal.CustomDataChanged -= Terminal_CustomDataChanged;
            _subscribedTerminals.Remove(terminal);
        }



        private void Terminal_CustomDataChanged(IMyTerminalBlock obj)
        {
            MyAPIGateway.Utilities.ShowMessage(ClassName,"Normal inventory block custom data change.");
            var block = (IMyCubeBlock)obj;

            // Check if the block is not designated as trash
            if (!Is_This_Trash_Block(obj))
            {
                // If the block was previously considered trash, remove it from the list
                if (_trashBlocks.Contains(block))
                {
                    _trashBlocks.Remove(block);
                }

                // Add the block to the inventory tracking
                Select_Blocks_With_Inventory(block);
            }
            else
            {
                // If the block is designated as trash but is already in the list
                if (_trashBlocks.Contains(block)) return;

                // Remove all inventories associated with the block
                Remove_Inventories_From_Storage(obj.InventoryCount, block);

                // Add the block to the trash list
                _trashBlocks.Add(block);
            }
        }
        private void MyCubeBlock_OnClosing(VRage.ModAPI.IMyEntity obj)
        {
            UnsubscribeBlock((IMyCubeBlock)obj);
            obj.OnClosing -= MyCubeBlock_OnClosing;
        }

    }
}