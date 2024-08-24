using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Logistics
{
    internal class GridScanner
    {
        private readonly IMyCubeGrid _imyCubeGrid;
        private BlockStorage _blockStorageInstance;
        private bool _errorOnLoading;

        public GridScanner(IMyCubeGrid imyCubeGrid, out BlockStorage blockStorageInstance)
        {
            _imyCubeGrid = imyCubeGrid;
            List<GridData> rawData;
            if (TryInitializeFromXml(out rawData)) InitializeFromXml(rawData);
            if (_errorOnLoading) ScanGridFull();


            blockStorageInstance = _blockStorageInstance;
        }

        private void ScanGridFull()
        {
            var assemblers = _imyCubeGrid.GetFatBlocks<IMyAssembler>().ToList();
            var cargoContainers = _imyCubeGrid.GetFatBlocks<IMyCargoContainer>().ToList();
            var refineries = _imyCubeGrid.GetFatBlocks<IMyRefinery>().ToList();
            var gasGenerators = _imyCubeGrid.GetFatBlocks<IMyGasGenerator>().ToList();
            var conveyorSorters = _imyCubeGrid.GetFatBlocks<IMyConveyorSorter>().ToList();
            var shipConnectors = _imyCubeGrid.GetFatBlocks<IMyShipConnector>().ToList();

            _blockStorageInstance = new BlockStorage(
                assemblers,
                refineries,
                gasGenerators,
                shipConnectors,
                conveyorSorters,
                cargoContainers,
                _imyCubeGrid
            );
        }
        
        private bool TryInitializeFromXml(out List<GridData> rawData)
        {
            rawData = GridDataSerializer.DeserializeFromXml(_imyCubeGrid.EntityId.ToString());
            return rawData != null;
        }

        private void InitializeFromXml(List<GridData> rawData)
        {
            var myAssembler = new List<IMyAssembler>();
            var myCargoContainer = new List<IMyCargoContainer>();
            var myRefinery = new List<IMyRefinery>();
            var myGasGenerator = new List<IMyGasGenerator>();
            var myConveyorSorter = new List<IMyConveyorSorter>();
            var myShipConnector = new List<IMyShipConnector>();

            try
            {
                foreach (var data in rawData)
                {
                    if(_errorOnLoading) return; 
                    switch (data.ObjectTypeString)
                    {
                        case "IMyAssembler":
                            myAssembler.AddRange(data.IdList.Select(GetBlockById)
                                .Where(block => block is IMyAssembler)
                                .Cast<IMyAssembler>());

                            break;

                        case "IMyCargoContainer":
                            myCargoContainer.AddRange(data.IdList.Select(GetBlockById)
                                .Where(block => block is IMyCargoContainer)
                                .Cast<IMyCargoContainer>());

                            break;

                        case "IMyRefinery":
                            myRefinery.AddRange(data.IdList.Select(GetBlockById).Where(block => block is IMyRefinery).Cast<IMyRefinery>());

                            break;

                        case "IMyGasGenerator":
                            myGasGenerator.AddRange(data.IdList.Select(GetBlockById)
                                .Where(block => block is IMyGasGenerator)
                                .Cast<IMyGasGenerator>());

                            break;

                        case "IMyConveyorSorter":
                            myConveyorSorter.AddRange(data.IdList.Select(GetBlockById)
                                .Where(block => block is IMyConveyorSorter)
                                .Cast<IMyConveyorSorter>());

                            break;

                        case "IMyShipConnector":
                            myShipConnector.AddRange(data.IdList.Select(GetBlockById)
                                .Where(block => block is IMyShipConnector)
                                .Cast<IMyShipConnector>());

                            break;
                    }
                }
                _blockStorageInstance = new BlockStorage(myAssembler, myRefinery, myGasGenerator, myShipConnector, myConveyorSorter, myCargoContainer, _imyCubeGrid);
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage("GridScanner",
                    $"An error occurred while scanning the grid: {ex.Message}");
            }
        }

        private IMyCubeBlock GetBlockById(long entityId)
        {
            var block = MyAPIGateway.Entities.GetEntityById(entityId) as IMyCubeBlock;
            if (block != null) return block;

            MyAPIGateway.Utilities.ShowMessage("GridScanner",
                $"An error occurred while scanning the grid, block {entityId} was not found");
            _errorOnLoading = true;
            return null;

        }
    }
}