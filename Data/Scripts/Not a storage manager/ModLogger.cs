using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager
{
    public class ModLogger
    {
        private readonly bool _isEnabled;
        public string GridId;
        private readonly string logFileName;

        public ModLogger(string logFileName, bool enableLogging = true)
        {
            _isEnabled = enableLogging;
            this.logFileName = logFileName;
            // Initialize the log file
            if (!_isEnabled) return;
            using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(logFileName, typeof(ModLogger)))
            {
                writer.WriteLine("----- Log Started: " + DateTime.Now + " -----");
            }
        }

        public void Log(string originClass, string message)
        {
            if (!_isEnabled) return;
            message = $"{GridId}::{originClass}: {message}";

            try
            {
                string existingContent;
                using (var stream = MyAPIGateway.Utilities.ReadFileInWorldStorage(logFileName, typeof(ModLogger)))
                {
                    existingContent = stream.ReadToEnd(); // Read the existing content
                    existingContent += $"{message}\n"; // Add new message with a newline
                    stream.Dispose(); // Dispose the read stream before writing
                }

                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(logFileName, typeof(ModLogger)))
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
            Log(originClass,"[WARNING]: " + message);
        }

        public void LogError(string originClass, string message)
        {
            Log(originClass,"[ERROR]: " + message);
        }
    }
}