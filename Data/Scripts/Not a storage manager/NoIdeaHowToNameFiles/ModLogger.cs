using System;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Scripting;
using VRage.Utils;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.NoIdeaHowToNameFiles
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class ModLogger : MySessionComponentBase
    {
        public bool IsEnabled = true;
        public bool IsSessionUnloading;


        public string ManagingBlockId = "";
        public static ModLogger Instance;
        private const string Ending = "_Logs.txt";

        public string LogFileName => ManagingBlockId + Ending;

        public override void LoadData()
        {
            base.LoadData();
            Instance = this;
            FirstMessage();
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            Log("Not a storage manager", "Session unloading");
            // Perform cleanup tasks here
        }
        private void ClearLog()
        {
            // This clears the log file by overwriting it with an empty string or an initial message
            using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(LogFileName, typeof(ModLogger)))
            {
                writer.Write(""); // Write an empty string to clear the log file
            }
        }
        private void FirstMessage()
        {
            ClearLog();
            var existingContent = "--------------Start of the logs-------------\n"; // Add a newline to the end of each message
            using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(LogFileName, typeof(ModLogger)))
            {
                writer.Write(existingContent); // Write back all the content including the new message
            }
        }
        public void Log(string originClass, string message)
        {
            if (!IsEnabled) return;

            message = $"{DateTime.Now}::{originClass}: {message}"; // Add a newline to the end of each message

            try
            {
                string existingContent;
                using (var stream = MyAPIGateway.Utilities.ReadFileInWorldStorage(LogFileName, typeof(ModLogger)))
                {
                    existingContent = stream.ReadToEnd(); // Read the existing content
                    existingContent += $"{message}\n"; // Add new message with a newline
                }

                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(LogFileName, typeof(ModLogger)))
                {
                    writer.Write(existingContent); // Write back all the content including the new message
                }
            }
            catch (Exception)
            {
                FirstMessage(); // Call FirstMessage to write the initial content if reading fails
                Log(originClass, message);
            }
        }

        public void LogWarning(string originClass, string message)
        {
            Log(originClass, "[WARNING]: " + message);
        }

        public void LogError(string originClass, string message)
        {
            Log(originClass, "[ERROR]: " + message);
        }
    }
}