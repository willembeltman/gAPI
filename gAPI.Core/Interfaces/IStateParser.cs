using Microsoft.Extensions.Primitives;

namespace gAPI.Core.Interfaces;

public interface IStateParser<TStateDto>
{
    public bool TryParse(string? value, out TStateDto state, int maxBufferSize);
    public string? ToStringBase64(TStateDto? value);
    public bool IsDifferent(TStateDto? value1, TStateDto? value2);
    public TStateDto? CreateCopy(TStateDto? value);

    public bool TryParse(StringValues values, out TStateDto state, int maxBufferSize)
        => TryParse(values.FirstOrDefault(), out state, maxBufferSize);
    public bool TryParse(IEnumerable<string> values, out TStateDto state, int maxBufferSize)
        => TryParse(values.FirstOrDefault(), out state, maxBufferSize);
    public StringValues ToStringValuesBase64(TStateDto value)
    {
        string? base64State = ToStringBase64(value);
        return new StringValues(base64State);
    }
}