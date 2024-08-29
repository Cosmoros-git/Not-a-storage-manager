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
        private readonly string _logFilePath;
        private readonly bool _isEnabled;
        public string GridId;

        public ModLogger(string logFileName, bool enableLogging = true)
        {
            _isEnabled = enableLogging;
            _logFilePath = Path.Combine(MyAPIGateway.Utilities.GamePaths.ModScopeName, logFileName);

            // Initialize the log file
            if (!_isEnabled) return;
            using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(_logFilePath, typeof(ModLogger)))
            {
                writer.WriteLine("----- Log Started: " + DateTime.Now + " -----");
            }
        }

        public void Log(string originClass, string message)
        {
            if (!_isEnabled) return;
            var sb = new StringBuilder();
            sb.Append(GridId);
            sb.Append(": ");
            sb.Append(originClass);
            sb.Append(": ");
            sb.Append(message);
            message = sb.ToString();

            try
            {
                using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(_logFilePath, typeof(ModLogger)))
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
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