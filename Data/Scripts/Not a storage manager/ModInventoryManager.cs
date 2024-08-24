using System;
using System.Linq;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.Entity;
using IMyInventory = VRage.Game.ModAPI.IMyInventory;

namespace Logistics
{
    public interface IModInventoryStorage
    {
        event Action<MyFixedPoint> OnFreeSpaceChanged;
        event Action<MyFixedPoint> OnTotalSpaceChanged;
        MyFixedPoint GetFreeSpace { get; set; }
        MyFixedPoint GetTotalSpace { get; set; }
    };


    internal class ModInventoryStorage : IGridBlockManager, IModInventoryStorage, IEndOfLife
    {

        //TODO OPTIMIZE INTO OBLIVION. THIS ENTIRE SCRIPT IS A MYSTERY.

        public event Action EndOfLifeTrigger;
        public event Action JustOnCloseTrigger;

        public event Action<MyCubeBlock> StorageBlockAdded;
        public event Action<MyCubeBlock> StorageBlockRemoved;

        public event Action<MyFixedPoint> OnFreeSpaceChanged;
        public event Action<MyFixedPoint> OnTotalSpaceChanged;

        private MyFixedPoint _totalSpace;
        private MyFixedPoint _freeSpace;

        public MyFixedPoint GetFreeSpace
        {
            get { return _freeSpace; }
            set
            {
                if (_freeSpace == value) return;
                _freeSpace = value;
                OnFreeSpaceChanged?.Invoke(_freeSpace);
            }
        }

        public MyFixedPoint GetTotalSpace
        {
            get { return _totalSpace; }
            set
            {
                if (_totalSpace == value) return;
                _totalSpace = value;
                OnTotalSpaceChanged?.Invoke(_totalSpace);
            }
        }

        private readonly BlockTypeCollection<IMyCargoContainer> _myContainersCargo;

        public ModInventoryStorage(BlockStorage blockStorage)
        {
            _myContainersCargo = blockStorage.CargoContainers;
            StorageBlockAdded += OnStorageBlockAdded;
            StorageBlockRemoved += OnStorageBlockRemoved;
            InitializeStorage();
            SubscribeToAllInventoryEvents();
            EndOfLifeTrigger += UnsubscribeFromAllInventoryEvents;
            JustOnCloseTrigger += UnsubscribeFromAllInventoryEvents;
        }

        private void SubscribeToAllInventoryEvents()
        {
            foreach (var cargo in _myContainersCargo.GetBlocks())
            {
                SubscribeToInventoryEvents(cargo);
            }
        }

        private void UnsubscribeFromAllInventoryEvents()

        {
            EndOfLifeTrigger -= UnsubscribeFromAllInventoryEvents;
            JustOnCloseTrigger -= UnsubscribeFromAllInventoryEvents;
            foreach (var cargo in _myContainersCargo.GetBlocks())
            {
                UnsubscribeFromInventoryEvents(cargo);
            }
        }


        private void SubscribeToInventoryEvents(IMyCargoContainer cargoContainer)
        {
            var inventory = (MyInventory)cargoContainer.GetInventory();
            if (inventory == null) return;
            inventory.ContentsChanged += OnContentsChanged;
        }

        private void UnsubscribeFromInventoryEvents(IMyCargoContainer cargoContainer)
        {
            var inventory = (MyInventory)cargoContainer.GetInventory();
            if (inventory == null) return;
            inventory.ContentsChanged -= OnContentsChanged;
        }

        
        private void InitializeStorage()
        {
            GetTotalSpace = CalculateTotalSpace();
            GetFreeSpace = CalculateFreeSpace();
        }

        private MyFixedPoint CalculateTotalSpace()
        {
            return _myContainersCargo.GetBlocks().Select(cargoContainer => cargoContainer.GetInventory())
                .Where(inventory => inventory != null)
                .Aggregate<IMyInventory, MyFixedPoint>(0, (current, inventory) => current + inventory.MaxVolume);
        }

        private MyFixedPoint CalculateFreeSpace()
        {
            return _myContainersCargo.GetBlocks().Select(cargoContainer => cargoContainer.GetInventory())
                .Where(inventory => inventory != null).Aggregate<IMyInventory, MyFixedPoint>(0,
                    (current, inventory) => current + (inventory.MaxVolume - inventory.CurrentVolume));
        }

        private void OnStorageBlockAdded(MyCubeBlock myCubeBlock)
        {
            var container = myCubeBlock as MyCargoContainer;
            var inventory = container?.GetInventory();
            if (inventory == null) return;
            SubscribeToInventoryEvents(container);
            GetTotalSpace += inventory.MaxVolume;
            GetFreeSpace += inventory.MaxVolume - inventory.CurrentVolume;
        }

        private void OnStorageBlockRemoved(MyCubeBlock myCubeBlock)
        {
            var container = myCubeBlock as MyCargoContainer;
            var inventory = container?.GetInventory();
            if (inventory == null) return;
            UnsubscribeFromInventoryEvents(container);
            GetTotalSpace -= inventory.MaxVolume;
            GetFreeSpace += inventory.MaxVolume - inventory.CurrentVolume;
        }

        
        private void OnContentsChanged(MyInventoryBase inventory)
        {
            CalculateFreeSpace();
        }

    }
}