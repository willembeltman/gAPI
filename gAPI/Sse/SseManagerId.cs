namespace gAPI.Sse
{
    public readonly struct SseManagerId
    {
        public SseManagerId(long value)
        {
            Value = value;
        }

        public long Value { get; }
    }
}