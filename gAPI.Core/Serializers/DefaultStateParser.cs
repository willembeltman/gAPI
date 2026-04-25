using gAPI.Dtos;
using gAPI.Interfaces;
using Microsoft.Extensions.Logging;

namespace gAPI.Serializers;

public class DefaultStateParser(ILoggerFactory loggerFactory) : IStateParser<AuthStateDto>
{
    public ILogger Logger { get; } = loggerFactory.CreateLogger<DefaultStateParser>();

    public bool TryParse(string? value, out AuthStateDto state)
    {
        state = default!;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            var data = Convert.FromBase64String(value);
            var offset = 0;
            try
            {
                state = data.ReadAuthStateDto(ref offset);
            }
            catch (Exception ex)
            {
                Logger.LogError("TryParse => Exception: {ex}", ex);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to parse state from string value.");
            return false;
        }
    }
    public string? ToStringBase64(AuthStateDto? value)
    {
        if (value == null)
            value = new AuthStateDto();

        byte[] Buffer = new byte[1024 * 64];
        var span = new Span<byte>(Buffer, 0, Buffer.Length);
        var offset = 0;
        span.Write(ref offset, value);
        var base64State = Convert.ToBase64String(Buffer, 0, offset);
        return base64State;
    }
    public bool IsDifferent(AuthStateDto? value1, AuthStateDto? value2)
    {
        if (value1 == null && value2 == null) return true;
        if (value1 == null || value2 == null) return false;
        return value1.IsDifferent(value2);
    }
    public AuthStateDto? CreateCopy(AuthStateDto? value)
    {
        return value?.CreateCopy();
    }
}
