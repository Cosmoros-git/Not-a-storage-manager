using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.CustomDataManager
{

    internal class TotallyATuple
    {
        public MyFixedPoint Limit { get; private set; }
        public float ToleranceRange { get; private set; }
        public MyFixedPoint MaxValue { get; private set; }

        public TotallyATuple(int limit, float toleranceRange)
        {
            Limit = limit;
            ToleranceRange = toleranceRange < 0 ? 0 : toleranceRange;

            var tolerance = limit * ToleranceRange / 100;

            MaxValue = (MyFixedPoint)(limit + tolerance);
        }
    }

}
