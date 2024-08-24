using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;

namespace Logistics
{
    // You know. At start this was just a small sorter project of mine. :C

    public interface IGridBlockManager
    {
        event Action<MyCubeBlock> StorageBlockAdded;
        event Action<MyCubeBlock> StorageBlockRemoved;
    }

    internal class GridBlockManager : IGridBlockManager
    {
        private readonly BlockStorage _scan;
        private long _gridId;

        public event Action<MyCubeBlock> StorageBlockAdded;
        public event Action<MyCubeBlock> StorageBlockRemoved;

        public GridBlockManager(BlockStorage scan, long originalGridId)
        {
            _scan = scan;
            _gridId = originalGridId;
        }

        public void OnFatBlockAdded(MyCubeBlock myCubeBlock)
        {
            string type;
            StaticBlockCategorizer.GetBlockType(myCubeBlock, out type);
            switch (type)
            {
                case "IMyCargoContainer": StorageBlockAdded?.Invoke(myCubeBlock);break;
            }
            
            StaticBlockCategorizer.GetBlockTypeAndAddToStorage(myCubeBlock, _scan);

        }

        public void OnFatBlockRemoved(MyCubeBlock myCubeBlock)
        {
            string type;
            StaticBlockCategorizer.GetBlockType(myCubeBlock, out type); // Watch this be never useful :D
            switch (type)
            {
                case "IMyCargoContainer": StorageBlockAdded?.Invoke(myCubeBlock); break;
            }
            StaticBlockCategorizer.GetBlockTypeAndRemoveFromStorage(myCubeBlock, _scan);
        }

        public void OnGridMerge(MyCubeGrid myCubeGrid, MyCubeGrid cubeGrid)
        {
            if (myCubeGrid.EntityId != _gridId)
            {
                _gridId = myCubeGrid.EntityId;
            }
            // Todo deals with grid fusion between managed grids etc.

        }

        public void OnGridSplit(MyCubeGrid myCubeGrid, MyCubeGrid cubeGrid)
        {

        }

    }
    /// <summary>
    /// Stores the scanned items for a grid the block is on. Should not store data of connected grids.
    /// NGL this is absolute fucking mess. The convoluted mess of data that is made SPECIFICALLY FOR ME. 
    /// </summary>
    public class BlockStorage
    {
        public IMyCubeGrid CubeGrid { get; }
        public long MyCubeGridId { get; }
        public long PcuData { get; set; }
        public BlockTypeCollection<IMyAssembler> Assemblers { get; private set; }
        public BlockTypeCollection<IMyRefinery> Refineries { get; private set; }
        public BlockTypeCollection<IMyGasGenerator> GasGenerators { get; private set; }
        public BlockTypeCollection<IMyShipConnector> ShipConnectors { get; private set; }
        public BlockTypeCollection<IMyConveyorSorter> ConveyorSorters { get; private set; }
        public BlockTypeCollection<IMyCargoContainer> CargoContainers { get; private set; }


        public BlockStorage(List<IMyAssembler> myAssemblers = null,
            List<IMyRefinery> myRefineries = null,
            List<IMyGasGenerator> myGasGenerators = null,
            List<IMyShipConnector> myShipConnectors = null,
            List<IMyConveyorSorter> myConveyorSorters = null,
            List<IMyCargoContainer> myCargoContainers = null,
            IMyCubeGrid cubeGrid = null)
        {
            Assemblers = new BlockTypeCollection<IMyAssembler>(myAssemblers);
            Refineries = new BlockTypeCollection<IMyRefinery>(myRefineries);
            GasGenerators = new BlockTypeCollection<IMyGasGenerator>(myGasGenerators);
            ShipConnectors = new BlockTypeCollection<IMyShipConnector>(myShipConnectors);
            ConveyorSorters = new BlockTypeCollection<IMyConveyorSorter>(myConveyorSorters);
            CargoContainers = new BlockTypeCollection<IMyCargoContainer>(myCargoContainers);
            CubeGrid = cubeGrid;
            if (CubeGrid != null) MyCubeGridId = CubeGrid.EntityId;
            // It keeps telling me how CubeGrid can be null.
            // HOW IT WILL BE NULL? IT CAN NOT BE NULL.IF ITS NULL SOMETHING MESSED UP BAD.
            GetPcu();
        }

        private void GetPcu()
        {
            if (CubeGrid != null)
            {
                var actualCubeGrid = CubeGrid as MyCubeGrid;
                if (actualCubeGrid != null)
                {
                    PcuData = actualCubeGrid.BlocksPCU;
                }
                else
                {
                    MyAPIGateway.Utilities.ShowMessage("DataMyGridScan",
                        "IMyCubeGrid could not be cast to MyCubeGrid.");
                }
            }
            else
            {
                MyAPIGateway.Utilities.ShowMessage("DataMyGridScan", "CubeGrid is null.");
            }
        }

        private void UpdateCpu(long pcu)
        {
            PcuData = pcu;
        }
    }
}