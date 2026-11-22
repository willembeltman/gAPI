using gAPI.Core.Attributes;
using gAPI.Core.Dtos;

namespace gAPI.Core.Server.Dtos;

[GenerateSerializer]
public record BusSubscribeAckDto(
    BusSubscribeDto SubscribeDto,
    bool Cancelled,
    ExceptionDto? Exception)
{
    public override string ToString()
    {
        return SubscribeDto.ToString();
    }
}