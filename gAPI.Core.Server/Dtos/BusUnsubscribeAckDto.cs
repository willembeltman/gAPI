using gAPI.Core.Attributes;
using gAPI.Core.Dtos;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record BusUnsubscribeAckDto(
    BusUnsubscribeDto UnsubscribeDto,
    bool Cancelled,
    ExceptionDto? Exception)
{
    public override string ToString()
    {
        // This string is a key
        return UnsubscribeDto.ToString();
    }
}