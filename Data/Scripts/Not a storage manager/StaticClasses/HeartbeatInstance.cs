using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.StaticClasses
{
    public static class HeartbeatInstance
    {
        public static event Action HeartBeat;
        public static event Action HeartBeat10;
        public static event Action HeartBeat100;

        public static void OnHeartBeat10()
        {
            HeartBeat10?.Invoke();
        }

        public static void OnHeartBeat100()
        {
            HeartBeat100?.Invoke();
        }

        public static void OnHeartBeat()
        {
            HeartBeat?.Invoke();
        }
    }
}
