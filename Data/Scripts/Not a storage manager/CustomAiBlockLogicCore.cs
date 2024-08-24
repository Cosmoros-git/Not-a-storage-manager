using System;
using System.Linq;
using Logistics.Data.Scripts.Not_a_storage_manager;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Logistics
{
    public interface IReScan
    {
        event Action ReScanTrigger;
        bool ReScanEnabled { get; set; }
    }
    public interface IEndOfLife
    {
        event Action EndOfLifeTrigger;
        event Action JustOnCloseTrigger;
    }

    public interface IHeartbeat
    {
        event Action HeartBeatOn1;
        event Action HeartBeatOn10;
        event Action HeartBeatOn100;
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TerminalBlock), false, "LargeSystemControlPanel",
        "SmallSystemControlPanel")]
    public class SystemBlockCore : MyGameLogicComponent, IHeartbeat, IEndOfLife, IReScan
    {
        private int _launchSequenceStep = -1; // Step in 10th update.
  

        private int _eventLinkStep;
        private long _ownerId = -1;
        private string _factionTag; // TODO for cases where people are in faction and want to share stuff
        private bool _isEndOfLife;
        private bool _secondaryManagerSituation;

        public event Action EndOfLifeTrigger;
        public event Action JustOnCloseTrigger;

        public event Action HeartBeatOn1;
        public event Action HeartBeatOn10;
        public event Action HeartBeatOn100;
        public event Action ReScanTrigger;
        public bool ReScanEnabled { get; set; } = false;

        private bool _hasEntityBeenInitialized; // If entity is valid
        private bool _hasDataBeenLoadedOrFilled; // If data has been scanned and initialized


        // Cube and grid references
        private IMyCubeBlock _iBlock;
        private MyCubeGrid _myCubeGrid;
        private IMyCubeGrid _iMyCubeGrid;
        private IMyTerminalBlock _iTerminalBlock;


        private BlockStorage _blockStorage; // This is data of the scan in the most cursed format.
        private GridBlockManager _gridBlockManager;
        private GridScanner _gridScanner;
        private ModInventoryStorage _modInventoryStorage;
        private ModProductionManager _modProductionManager;
        private static readonly Guid ModId = new Guid("e7a734a2-c3a3-4a3f-b4a7-b03d7a3bb59e");
        private string guide;

        public void AssignManagerBlock(IMyCubeGrid grid)
        {
            var storage = grid.Storage ?? new MyModStorageComponent();

            // Save the manager block's EntityId to the grid's storage with the static ModId key
            storage.SetValue(ModId, _iBlock.EntityId.ToString());

            grid.Storage = storage;
        }

        public bool CheckIfThisIsManagerBlock(IMyCubeGrid grid)
        {
            string storedValue;

            if (grid.Storage == null || !grid.Storage.TryGetValue(ModId, out storedValue))
                return false; // Return false if the key doesn't exist or parsing fails
            long managerEntityId;
            if (long.TryParse(storedValue, out managerEntityId))
            {
                // Compare managerEntityId with the current block's EntityId
                return _iBlock.EntityId == managerEntityId;
            }

            return false; // Return false if the key doesn't exist or parsing fails
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            try
            {
                base.Init(objectBuilder);
                // Delay the physics check to the next frame otherwise fails on loading into the save.
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
            catch (Exception ex)
            {
                MyAPIGateway.Utilities.ShowMessage("CustomAIBlock", $"Block Initialization failed: {ex.Message}");
            }
        }

        private void CheckEntityValidity()
        {
            _iBlock = (IMyCubeBlock)Entity;
            _iMyCubeGrid = _iBlock.CubeGrid;
            _myCubeGrid = (MyCubeGrid)_iMyCubeGrid;

            if (_iBlock == null || _myCubeGrid == null)
            {
                MyAPIGateway.Utilities.ShowMessage("CustomAIBlock", "Block or Grid is null.");
                return;
            }

            if (_myCubeGrid.Physics == null)
            {
                MyAPIGateway.Utilities.ShowMessage("CustomAIBlock",
                    "Grid has no physics. Retrying in the next frame...");
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME; // Keep checking in the next frame
                return;
            }

            _ownerId = _iBlock.OwnerId;
            _factionTag = _iBlock.GetOwnerFactionTag();
            if (!CheckIfThisIsManagerBlock(_iMyCubeGrid))
            {
                _secondaryManagerSituation = true;
                Close();
            }


            MyAPIGateway.Utilities.ShowMessage("CustomAIBlock",
                $"Grid: {_myCubeGrid.DisplayName}, OwnerId: {_ownerId}, Faction Tag: {_factionTag}");

            _iTerminalBlock = (IMyTerminalBlock)_iBlock;
            _hasEntityBeenInitialized = true;
            CreateGuide();
        }

        private void CreateGuide()
        {
            guide = @"
<<< Trash tags guide >>>
This part deals with over-quota items 
[D] = Disassemble Over Quota.
Requires an Assembler set to disassemble. With IO it's machine specific.
[I] = Incinerate Over Quota, with IO Incinerator. 
[T] = Trash, finds connector with tag [TRASH], and it's used like a dumpster.
[S] = Finds the storage container with tag [SELL] and stores them in it. 
If they can’t find it, they will get rid of them if, somehow, they keep being produced.

<<< Production tags guide >>>
[Unique] = Make once and remove.
Example: Make a Grinder once and be done with it.
<Elite Grinder> 1 [Unique]>
[Show] = Show on the production screen. On default, it's not shown. 
At this, I run out of ideas; what else do you want? Gnomes?

<<Storage tags guide>>

[COMPONENTS] tag in the name will tag storage as one for components. It has a natural priority of 2.
[ORES] tag will put ores in this storage. It will not try to suck every single ore out of everywhere. 
It is only out of storage.
[INGOTS] No comment on this one.

In the tagged storages, there will be lists of components/ores/ingots and the number you want in VOLUME, not the amount. 
If you decide to force more into it than can fit, well, it won't do that. It will be first come first fill.

[Push] = true. It will make my manager try to always keep it empty for your fast load-out needs.
[Spread] = true [Spread group] = groupName 
This will make the manager try to spread between storages in the same group name.

<<< Production values paste them into an appropriate LCD you want them to be shown at. >>>
<<< I AM HORRIBLE WITH UI. I WOULD RATHER EAT GLASS THAN DEAL WITH UI. BE WARNED >>> 
This is just a list of items you can make. Copy it and paste it into the custom data of LCD with 
a tag [NASM] there you can specify production amounts. 
If the number of items to show goes above 18, they will be shown on the next LCD in line with the tag and appropriate whatever.
";
            guide += GetCraftersAndCraftees.Instance.BlueprintsForGuide;
        }
        public void SetGuideToCustomData()
        {
            _iTerminalBlock.CustomData = guide;
            _iTerminalBlock.RefreshCustomInfo();
        }


        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (!_hasEntityBeenInitialized) CheckEntityValidity(); // Grid Validity Check
            AssignManagerBlock(_iMyCubeGrid); // Adds manager ID to storage.

            // Once the grid is confirmed, starts the update loops on frames that are tied to Interface with events.
            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME |
                          MyEntityUpdateEnum.EACH_FRAME_AFTER; // Enables updates on 1,10,100;
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME; // Disables the each frame update.
        } // Manages startup sequence


        public override void UpdateAfterSimulation()
        {
            base.UpdateBeforeSimulation();
            HeartBeatOn1?.Invoke();
        }


        private void ModLaunchSequence()
        {
            switch (_launchSequenceStep)
            {
                case -1:
                    if (_gridScanner == null)
                    {
                        EventLinking(false);
                        MyAPIGateway.Utilities.ShowMessage("CustomAIBlock", $"Loading in data.");
                        _gridScanner = new GridScanner(_iMyCubeGrid, out _blockStorage);
                        if (_gridScanner == null)
                        {
                            MyAPIGateway.Utilities.ShowMessage("CustomAIBlock", $"Critical error on gridScanner initialization. Shutting down.");
                            Close();
                            _secondaryManagerSituation = true;
                        }
                        _gridBlockManager = new GridBlockManager(_blockStorage, _myCubeGrid.EntityId);
                        _hasDataBeenLoadedOrFilled = true;
                        _launchSequenceStep++;
                    }

                    break;
                case 0:
                    if (_hasDataBeenLoadedOrFilled) _eventLinkStep++;
                    break; //Does first large grid scan

                case 1:
                    DebugTest();
                    _modInventoryStorage = new ModInventoryStorage(_blockStorage);
                    _modProductionManager = new ModProductionManager(_iMyCubeGrid,_blockStorage,_modInventoryStorage);
                    _launchSequenceStep++;
                    break;
            }
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();
            HeartBeatOn10?.Invoke();
            ModLaunchSequence();
        }

        private void GetControlLab(){

        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            SetGuideToCustomData();
            HeartBeatOn100?.Invoke();
        }


        private void OnOwnerChanged(MyCubeGrid myCubeGrid)
        {
            _ownerId = _iBlock.OwnerId;
        }

        public void EventLinking(bool unlink)
        {
            switch (_eventLinkStep)
            {
                case 0:
                    _myCubeGrid.OnBlockOwnershipChanged += OnOwnerChanged;

                    break;
                case 1:
                    _myCubeGrid.OnFatBlockAdded += _gridBlockManager.OnFatBlockAdded;
                    _myCubeGrid.OnFatBlockRemoved += _gridBlockManager.OnFatBlockRemoved;
                    _myCubeGrid.OnGridMerge += _gridBlockManager.OnGridMerge;
                    _myCubeGrid.OnGridSplit += _gridBlockManager.OnGridSplit;
                    _iBlock.OnMarkForClose += EndOfLife;
                    break;
            }

            if (!unlink) return;
            _myCubeGrid.OnFatBlockAdded -= _gridBlockManager.OnFatBlockAdded;
            _myCubeGrid.OnFatBlockRemoved -= _gridBlockManager.OnFatBlockRemoved;
            _iBlock.OnMarkForClose -= EndOfLife;
            _myCubeGrid.OnGridMerge -= _gridBlockManager.OnGridMerge;
            _myCubeGrid.OnGridSplit -= _gridBlockManager.OnGridSplit;
            _myCubeGrid.OnBlockOwnershipChanged -= OnOwnerChanged;
        }


        public void DebugTest()
        {
            MyAPIGateway.Utilities.ShowMessage("CustomAIBlock", $"Debug storage data");

            if (_blockStorage != null)
            {
                // Count and display the number of refineries
                var refineriesCount = _blockStorage.Refineries.Count;
                MyAPIGateway.Utilities.ShowMessage("CustomAIBlock", $"Refineries count: {refineriesCount}");

                // Count and display the number of assemblers
                var assemblersCount = _blockStorage.Assemblers.Count;
                MyAPIGateway.Utilities.ShowMessage("CustomAIBlock", $"Assemblers count: {assemblersCount}");

                // Count and display the number of cargo containers
                var cargoContainersCount = _blockStorage.CargoContainers.Count;
                MyAPIGateway.Utilities.ShowMessage("CustomAIBlock", $"Cargo Containers count: {cargoContainersCount}");

                // Count and display the number of gas generators
                var gasGeneratorsCount = _blockStorage.GasGenerators.Count;
                MyAPIGateway.Utilities.ShowMessage("CustomAIBlock", $"Gas Generators count: {gasGeneratorsCount}");

                // Count and display the number of conveyor sorters
                var conveyorSortersCount = _blockStorage.ConveyorSorters.Count;
                MyAPIGateway.Utilities.ShowMessage("CustomAIBlock", $"Conveyor Sorters count: {conveyorSortersCount}");

                // Count and display the number of ship connectors
                var shipConnectorsCount = _blockStorage.ShipConnectors.Count;
                MyAPIGateway.Utilities.ShowMessage("CustomAIBlock", $"Ship Connectors count: {shipConnectorsCount}");
            }
            else
            {
                MyAPIGateway.Utilities.ShowMessage("CustomAIBlock", $"Storage is null");
            }
        } // MINE DON'T TOUCH.
        public override void Close()
        {
            if (!_isEndOfLife)
            {
                if (_secondaryManagerSituation)
                {
                    //Do not save or delete
                }
                //new PhysicalStorageManager(_myGridScanData, _myCubeGrid); // Saves data into Json.
                NeedsUpdate = MyEntityUpdateEnum.NONE;
                EventLinking(true);
                JustOnCloseTrigger?.Invoke();
            }

            base.Close();
        }

        private void EndOfLife(IMyEntity myEntity)
        {
            _isEndOfLife = true;
            EndOfLifeTrigger?.Invoke();
            MyAPIGateway.Utilities.ShowMessage("CustomAIBlock", "Block has been destroyed.");
            //new PhysicalStorageManager(_myCubeGrid.EntityId); // Deletes the Json from storage.
            EventLinking(true);
            NeedsUpdate = MyEntityUpdateEnum.NONE;
        }
    }
}