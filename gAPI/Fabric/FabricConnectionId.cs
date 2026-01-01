namespace gAPI.Fabric
{
    public readonly struct FabricHostId
    {
        public FabricHostId(long value)
        {
            Value = value;
        }

        public long Value { get; }
    }
}