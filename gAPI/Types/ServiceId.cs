namespace gAPI.Types
{
    public readonly struct ServiceId
    {
        public ServiceId(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}