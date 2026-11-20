using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record PlaneUnsubscribeAckDto(
    PlaneUnsubscribeDto UnsubscribeDto,
    bool Cancelled,
    string? ExceptionMessage,
    string? InnerExceptionMessage)
{
    public override string ToString()
    {
        // This string is a key
        return UnsubscribeDto.ToString();
    }
}