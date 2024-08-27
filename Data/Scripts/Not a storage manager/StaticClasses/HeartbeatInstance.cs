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
        public static event Action HeartBeat1000;
        private static int _heartBeatCount;


        public static void OnHeartBeat1000()
        {
            HeartBeat1000?.Invoke();
        }
        public static void OnHeartBeat100()
        {
           
            HeartBeat100?.Invoke();
            if (_heartBeatCount < 10)
            {
                _heartBeatCount++;
            }
            else
            {
                // Custom rarer update.
                OnHeartBeat1000();
            }
        }
        public static void OnHeartBeat10()
        {
            HeartBeat10?.Invoke();
        }
        public static void OnHeartBeat()
        {
            HeartBeat?.Invoke();
        }
    }
}
