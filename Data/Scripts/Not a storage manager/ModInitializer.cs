using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.AbstractClass;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.GridAndBlockManagers;
using NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager
{
    public class ModInitializer : ModBase
    {
        private int _loadingStep = 1;

        private readonly IMyEntity _entity;
        private readonly IMyCubeBlock _varImyCubeBlock;
        private readonly IMyCubeGrid _varIMyCubeGrid;

        public GlobalStorageInstance GlobalStorageInstance;
        public GridScannerManager VarGridScannerManager;

        public ModInitializer(IMyEntity entity)
        {
            _entity = entity;
            _varImyCubeBlock = (IMyCubeBlock)entity;
            _varIMyCubeGrid = _varImyCubeBlock.CubeGrid;
        }

        public bool VerifyLaunch()
        {
            if (_varImyCubeBlock == null || _varIMyCubeGrid == null)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName, "Block or Grid is null.");
                return false;
            }

            if (_varIMyCubeGrid.Physics == null)
            {
                MyAPIGateway.Utilities.ShowMessage(ClassName,
                    "Grid has no physics. Retrying in the next frame...");
            }

            MyAPIGateway.Utilities.ShowMessage(ClassName,
                $"Grid: {_varIMyCubeGrid.DisplayName}, OwnerId: {_varImyCubeBlock.OwnerId}, Faction Tag: {_varImyCubeBlock.GetOwnerFactionTag()}");

            HeartbeatInstance.HeartBeat10 += LoadingSequence;
            return true;
        }

        public void LoadingSequence()
        {
            switch (_loadingStep)
            {
                case 1:
                    GlobalStorageInstance = new GlobalStorageInstance();
                    MyAPIGateway.Utilities.ShowMessage(ClassName,
                        $"Loading sequence step 1");
                    _loadingStep++;
                    break;

                case 2:
                    if (VarGridScannerManager != null)
                    {
                        _loadingStep++;
                        return;
                    }
                    VarGridScannerManager = new GridScannerManager(_entity);
                    MyAPIGateway.Utilities.ShowMessage(ClassName,
                        $"Loading sequence step 2");
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
                case 6:
                    break;
            }
        }
    }
}