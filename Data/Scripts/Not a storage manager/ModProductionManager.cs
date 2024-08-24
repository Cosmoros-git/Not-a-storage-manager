using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageRender.Messages;

namespace Logistics.Data.Scripts.Not_a_storage_manager
{
    internal class ModProductionManager: IHeartbeat, IEndOfLife
    {
        public event Action HeartBeatOn1;
        public event Action HeartBeatOn10;
        public event Action HeartBeatOn100;

        public event Action EndOfLifeTrigger;
        public event Action JustOnCloseTrigger;



        private string _quotaSettings = $@"[Panel ID] = 
                                       [Panel SubId] = 
                                       <Production>";
        private List<IMyTextPanel> MyQuotaPanels;
        private readonly IMyCubeGrid _iMyCubeGrid;
        private readonly BlockStorage _blockStorage;
        private readonly ModInventoryStorage _modInventoryStorage;

        public ModProductionManager(IMyCubeGrid iMyCubeGrid, BlockStorage blockStorage,
            ModInventoryStorage modInventoryStorage)
        {
            _iMyCubeGrid = iMyCubeGrid;
            _blockStorage = blockStorage;
            _modInventoryStorage = modInventoryStorage;
            LinkEvents();
            
        }
        // The more I work on it the less I know what I want :D
        private void FindMyLcdInit()
        {
            var textPanels = _blockStorage.TextPanels.GetBlocks();
            if (_blockStorage.TextPanels.Count <= 0) return;
            foreach (var textPanel in textPanels )
            {
                if (!textPanel.CustomName.Contains("[NASM]")) continue;
                if(MyQuotaPanels.Contains(textPanel)) return;
                MyQuotaPanels.Add(textPanel);
                AddCustomDataToLcd(textPanel);
            }
        }

        private void LinkEvents()
        {
            HeartBeatOn1 += OnAfterSimulation1;
            HeartBeatOn10 += OnAfterSimulation10;
            HeartBeatOn100 += OnAfterSimulation100;
            EndOfLifeTrigger += UnlinkEvents;
        }

        private void UnlinkEvents()
        {
            HeartBeatOn1 -= OnAfterSimulation1;
            HeartBeatOn10 -= OnAfterSimulation10;
            HeartBeatOn100 -= OnAfterSimulation100;
            EndOfLifeTrigger -= UnlinkEvents;
        }

        private void AddCustomDataToLcd(IMyTextPanel textPanel)
        {
            if(textPanel.CustomData != "") return;
            // textPanel.CustomData = quotas settings
        }

        private void OnAfterSimulation1()
        {

        }
        private void OnAfterSimulation10()
        {
            //scan for 
        }
        private void OnAfterSimulation100()
        {

        }


        private void ProductionParse()
        {
            var queries = new StringBuilder();

            foreach (var panel in MyQuotaPanels)
            {
                // Assume panel.CustomData contains the text from the LCD
                var lcdText = panel.CustomData;

                // Split the text into lines
                var lines = lcdText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // Flag to start reading after "<Production>"
                var startReading = false;

                foreach (var line in lines)
                {
                    if (startReading)
                    {
                        // Add subsequent lines to queries StringBuilder
                        queries.AppendLine(line);
                    }
                    else if (line.Contains("<Production>"))
                    {
                        // Set the flag to start reading after this line
                        startReading = true;
                    }
                }
            }
        }


    }
}
