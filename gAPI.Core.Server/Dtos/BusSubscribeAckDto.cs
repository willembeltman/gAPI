using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record BusSubscribeAckDto(
    BusSubscribeDto SubscribeDto,
    bool Cancelled,
    string? ExceptionMessage)
{
    public override string ToString()
    {
        return SubscribeDto.ToString();
    }
}