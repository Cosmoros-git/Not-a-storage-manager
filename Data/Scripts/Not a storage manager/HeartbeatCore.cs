using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "LargeTrashSorter",
        "SmallTrashSorter")]
    public class HeartbeatCore : MyGameLogicComponent
    {
        private IMyCubeBlock _iBlock;
        private IMyCubeGrid _iMyCubeGrid;
        private MyCubeGrid _myCubeGrid;

        private ModInitializer _iModInitializer;


        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            _iModInitializer = new ModInitializer(Entity);
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (!_iModInitializer.VerifyLaunch()) NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME_AFTER |
                          MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            HeartbeatInstance.OnHeartBeat();
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();
            HeartbeatInstance.OnHeartBeat10();
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            HeartbeatInstance.OnHeartBeat100();
        }


        public override void Close()
        {
            //new PhysicalStorageManager(_myGridScanData, _myCubeGrid); // Saves data into Json.
            NeedsUpdate = MyEntityUpdateEnum.NONE;
            base.Close();
        }
    }
}