using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Logistics
{
    internal class GridScanner
    {
        public List<IMyAssembler> MyAssembler = new List<IMyAssembler>();
        public List<IMyCargoContainer> MyCargoContainer = new List<IMyCargoContainer>();
        public List<IMyRefinery> MyRefinery = new List<IMyRefinery>();
        public List<IMyGasGenerator> MyGasGenerator = new List<IMyGasGenerator>();
        public List<IMyConveyorSorter> MyConveyorSorter = new List<IMyConveyorSorter>();
        public List<IMyShipConnector> MyShipConnector = new List<IMyShipConnector>();
        public List<IMyTextPanel> MyTextPanels = new List<IMyTextPanel>();
        public List<IMyCubeBlock> MyOtherInventories = new List<IMyCubeBlock>();

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
            var fatBlocks = _imyCubeGrid.GetFatBlocks<IMyCubeBlock>();
            foreach (var fat in fatBlocks)
            {
                var assembler = fat as IMyAssembler;
                if (assembler != null)
                {
                    MyAssembler.Add(assembler);
                    continue;
                }

                var cargo = fat as IMyCargoContainer;
                if (cargo != null)
                {
                    MyCargoContainer.Add(cargo);
                    continue;
                }

                var refiner = fat as IMyRefinery;
                if (refiner != null)
                {
                    MyRefinery.Add(refiner);
                    continue;
                }

                var gas = fat as IMyGasGenerator;
                if (gas != null)
                {
                    MyGasGenerator.Add(gas);
                    continue;
                }

                var conveyorSort = fat as IMyConveyorSorter;
                if (conveyorSort != null)
                {
                    MyConveyorSorter.Add(conveyorSort);
                    continue;
                }

                var shipConnect = fat as IMyShipConnector;
                if (shipConnect != null)
                {
                    MyShipConnector.Add(shipConnect);
                    continue;
                }

                var text = fat as IMyTextPanel;
                if (text != null)
                {
                    MyTextPanels.Add(text);
                    continue;
                }

                if (fat.HasInventory)
                {
                    MyOtherInventories.Add(fat);
                }
            }

            if (_blockStorageInstance == null)
            {
                _blockStorageInstance = new BlockStorage(
                    MyAssembler, MyRefinery, MyGasGenerator,
                    MyShipConnector, MyConveyorSorter, MyCargoContainer,
                    MyTextPanels,MyOtherInventories, _imyCubeGrid
                );
            }
            else
            {
                _blockStorageInstance.UpdateAllBlocks(
                    MyAssembler, MyRefinery, MyGasGenerator,
                    MyShipConnector, MyConveyorSorter, MyCargoContainer,
                    MyTextPanels, MyOtherInventories
                );
            }

        }

        private bool TryInitializeFromXml(out List<GridData> rawData)
        {
            rawData = GridDataSerializer.DeserializeFromXml(_imyCubeGrid.EntityId.ToString());
            return rawData != null;
        }

        private void InitializeFromXml(List<GridData> rawData)
        {
            try
            {
                foreach (var data in rawData)
                {
                    if (_errorOnLoading) return;
                    switch (data.ObjectTypeString)
                    {
                        case "IMyAssembler":
                            MyAssembler.AddRange(data.IdList.Select(GetBlockById)
                                .Where(block => block is IMyAssembler)
                                .Cast<IMyAssembler>());

                            break;

                        case "IMyCargoContainer":
                            MyCargoContainer.AddRange(data.IdList.Select(GetBlockById)
                                .Where(block => block is IMyCargoContainer)
                                .Cast<IMyCargoContainer>());

                            break;

                        case "IMyRefinery":
                            MyRefinery.AddRange(data.IdList.Select(GetBlockById)
                                .Where(block => block is IMyRefinery)
                                .Cast<IMyRefinery>());

                            break;

                        case "IMyGasGenerator":
                            MyGasGenerator.AddRange(data.IdList.Select(GetBlockById)
                                .Where(block => block is IMyGasGenerator)
                                .Cast<IMyGasGenerator>());

                            break;

                        case "IMyConveyorSorter":
                            MyConveyorSorter.AddRange(data.IdList.Select(GetBlockById)
                                .Where(block => block is IMyConveyorSorter)
                                .Cast<IMyConveyorSorter>());

                            break;

                        case "IMyShipConnector":
                            MyShipConnector.AddRange(data.IdList.Select(GetBlockById)
                                .Where(block => block is IMyShipConnector)
                                .Cast<IMyShipConnector>());

                            break;
                        case "IMyTextPanel":
                            MyTextPanels.AddRange(data.IdList.Select(GetBlockById)
                                .Where(block => block is IMyTextPanel)
                                .Cast<IMyTextPanel>());

                            break;
                    }
                }

                if (_blockStorageInstance == null)
                {
                    _blockStorageInstance = new BlockStorage(
                        MyAssembler, MyRefinery, MyGasGenerator,
                        MyShipConnector, MyConveyorSorter, MyCargoContainer,
                        MyTextPanels, MyOtherInventories,_imyCubeGrid
                    );
                }
                else
                {
                    _blockStorageInstance.UpdateAllBlocks(
                        MyAssembler, MyRefinery, MyGasGenerator,
                        MyShipConnector, MyConveyorSorter, MyCargoContainer,
                        MyTextPanels, MyOtherInventories
                    );
                }

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