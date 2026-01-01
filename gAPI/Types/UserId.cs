namespace gAPI.Types
{
    public readonly struct UserId
    {
        public UserId(string? value)
        {
            Value = value;
        }

        public string? Value { get; }
    }
}