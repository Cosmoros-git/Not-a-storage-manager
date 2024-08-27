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
using Sandbox.Game.EntityComponents;

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
        public bool IsThereGridManagerOverlap;


        public ModInitializer(IMyEntity entity)
        {
            _entity = entity;
            _varImyCubeBlock = (IMyCubeBlock)entity;
            _varIMyCubeGrid = _varImyCubeBlock.CubeGrid;
        }

        private static readonly Guid ManagedKey = new Guid("MyTrashManager300000AndOutsiderTrading"); // Insert unique GUID :D

        // Check if the grid is already managed
        public bool IsGridManagedByThisBlock(IMyCubeGrid grid)
        {
            string storedValue;
            if (grid.Storage == null || !grid.Storage.TryGetValue(ManagedKey, out storedValue)) return false;
            if (!string.IsNullOrEmpty(storedValue)) return storedValue == _varImyCubeBlock.EntityId.ToString();
            MarkGridAsManaged(_varIMyCubeGrid);
            return true;
        }

        // Mark the grid as managed
        public void MarkGridAsManaged(IMyCubeGrid grid)
        {
            if (grid.Storage == null)
            {
                grid.Storage = new MyModStorageComponent();
            }

            grid.Storage.SetValue(ManagedKey, _varImyCubeBlock.EntityId.ToString());
        }


        public bool VerifyLaunch()
        {
            if (!IsThereGridManagerOverlap)
            {
                if (_varImyCubeBlock == null || _varIMyCubeGrid == null)
                {
                    MyAPIGateway.Utilities.ShowMessage(ClassName, "Block or Grid is null.");
                    return false;
                }

                if (_varIMyCubeGrid.Physics == null)
                {
                    MyAPIGateway.Utilities.ShowMessage(ClassName, "Grid has no physics. Retrying in the next frame...");
                    return false;
                }

                if (_varIMyCubeGrid.Storage == null)
                {
                    MarkGridAsManaged(_varIMyCubeGrid);
                    IsThereGridManagerOverlap = false;
                }
                else if (!IsGridManagedByThisBlock(_varIMyCubeGrid))
                {
                    IsThereGridManagerOverlap = true;
                    return false;
                }
            }
            else if (_varIMyCubeGrid.Storage == null || !IsGridManagedByThisBlock(_varIMyCubeGrid))
            {
                IsThereGridManagerOverlap = true;
                return false;
            }
            else
            {
                IsThereGridManagerOverlap = false;
            }

            return true; // Indicate that the grid was successfully initialized or is already managed


            MyAPIGateway.Utilities.ShowMessage(ClassName,
                $"Grid: {_varIMyCubeGrid.DisplayName}, OwnerId: {_varImyCubeBlock.OwnerId}, Faction Tag: {_varImyCubeBlock.GetOwnerFactionTag()}");

            HeartbeatInstance.HeartBeat100 += LoadingSequence;
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