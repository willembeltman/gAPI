using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record PlaneSubscribeAckDto(
    PlaneSubscribeDto SubscribeDto,
    bool Cancelled,
    string? ExceptionMessage,
    string? InnerExceptionMessage)
{
    public override string ToString()
    {
        return SubscribeDto.ToString();
    }
}