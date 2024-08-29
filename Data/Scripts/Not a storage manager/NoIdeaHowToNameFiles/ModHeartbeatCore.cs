using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.NoIdeaHowToNameFiles
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MyProgrammableBlock), false, "LargeTrashController",
        "SmallTrashController")]
    public class ModHeartbeatCore : MyGameLogicComponent
    {
        private int _amountOfLaunchTries;

        private int _initializationAttemptCounter;
        private const int AttemptsPerMinute = 60 * 60 / 100;
        private bool _initialized;

        private bool _isItOnStandBy;
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
            if (_initialized) return;
            try
            {
                ModLogger.Instance.ManagingBlockId = Entity.EntityId.ToString();
                if (!_iModInitializer.VerifyLaunch())
                {
                    if (_amountOfLaunchTries == 0)
                        ModLogger.Instance.LogError("Heartbeat core", "Verify launch failed. Trying again.");
                    if (_isItOnStandBy) return;
                    _amountOfLaunchTries++;

                    if (_amountOfLaunchTries <= 10)
                    {
                        // Try again on the next frame
                        NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                    }
                    else
                    {
                        // Switch to checking every 100th frame after 20 failed attempts
                        NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
                        _isItOnStandBy = true;
                    }

                    return;
                }
                // Initialization successful
                _initialized = true;

                // Continue updating at regular intervals as needed
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME |
                              MyEntityUpdateEnum.EACH_100TH_FRAME;
            }
            catch (Exception)
            {
                MyAPIGateway.Utilities.ShowMessage("Heartbeat core", "ModLogger is fucking null peace of shit");
                return;
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            ModHeartRate.OnHeartBeat();
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();
            ModHeartRate.OnHeartBeat10();
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            ModHeartRate.OnHeartBeat100();


            if (_initialized) return;

            _initializationAttemptCounter++;
            if (_initializationAttemptCounter < AttemptsPerMinute) return;

            // Reset the counter after one minute
            _initializationAttemptCounter = 0;

            // Try to initialize
            // Trigger another update attempt in the next frame
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void Close()
        {
            //new PhysicalStorageManager(_myGridScanData, _myCubeGrid); // Saves data into Json.
            _iModInitializer.Dispose();
            NeedsUpdate = MyEntityUpdateEnum.NONE;
            ModLogger.Instance?.Log("ModHeartBeatCore", "Close called.");

            base.Close();
        }
    }
}