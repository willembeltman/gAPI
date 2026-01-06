namespace gAPI.Types
{
    public readonly struct SessionId
    {
        public SessionId(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public override string ToString()
        {
            return Value;
        }
    }
}