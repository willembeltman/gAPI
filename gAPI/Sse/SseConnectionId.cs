namespace gAPI.Sse
{
    public readonly struct SseHostId
    {
        public SseHostId(long value)
        {
            Value = value;
        }

        public long Value { get; }
    }
}