using VRage;

namespace NotAStorageManager.Data.Scripts.Not_a_storage_manager.StorageSubclasses
{
    public struct ModTuple
    {
        public MyFixedPoint Limit { get; set; }
        public float ToleranceRange { get; set; }
        public MyFixedPoint MaxValue { get; private set; }

        public ModTuple(int limit, float toleranceRange)
        {
            Limit = limit;
            ToleranceRange = toleranceRange < 0 ? 0 : toleranceRange;

            var tolerance = limit * ToleranceRange / 100;

            MaxValue = (MyFixedPoint)(limit + tolerance);
        }
    }

}
