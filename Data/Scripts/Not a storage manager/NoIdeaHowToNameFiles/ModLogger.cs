using System;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Scripting;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.NoIdeaHowToNameFiles
{
    public class ModLogger : MySessionComponentBase
    {
        public bool IsEnabled;
        public bool IsSessionUnloading;


        public string ManagingBlockId = "No manager";
        private string _realLogName;
        public static ModLogger Instance { get; private set; }


        public string LogFileName
        {
            get { return _realLogName; }
            set { _realLogName = ManagingBlockId + value; }
        }

        public ModLogger()
        {
            using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(LogFileName, typeof(ModLogger)))
            {
                writer.WriteLine("----- Log Started: " + DateTime.Now + " -----");
            }

            Instance = this;
        }

        public void Log(string originClass, string message)
        {
            if (!IsEnabled) return;
            message = $"{ManagingBlockId}::{originClass}: {message}";

            try
            {
                string existingContent;
                using (var stream = MyAPIGateway.Utilities.ReadFileInWorldStorage(LogFileName, typeof(ModLogger)))
                {
                    existingContent = stream.ReadToEnd(); // Read the existing content
                    existingContent += $"{message}\n"; // Add new message with a newline
                    stream.Dispose(); // Dispose the read stream before writing
                }

                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(LogFileName, typeof(ModLogger)))
                {
                    writer.Write(existingContent); // Write back all the content including the new message
                }
            }
            catch (Exception ex)
            {
                // Handle any errors that occur when writing to the log file
                MyAPIGateway.Utilities.ShowMessage("Logger Error", ex.ToString());
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

        public override void LoadData()
        {
            base.LoadData();
            Log("Not a storage manager", "Session loading");
            // Perform setup tasks
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            Log("Not a storage manager", "Session unloading");
            // Perform cleanup tasks here
        }
    }
}