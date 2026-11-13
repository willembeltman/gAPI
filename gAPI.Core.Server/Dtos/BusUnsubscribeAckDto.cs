using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record BusUnsubscribeAckDto(
    BusUnsubscribeDto UnsubscribeDto,
    bool Cancelled,
    string? ExceptionMessage)
{
    public override string ToString()
    {
        // This string is a key
        return UnsubscribeDto.ToString();
    }
}