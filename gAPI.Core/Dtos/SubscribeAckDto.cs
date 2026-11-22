using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record SubscribeAckDto(
    SubscribeDto SubscribeDto,
    bool Cancelled,
    ExceptionDto? Exception)
{
    public override string ToString()
    {
        return SubscribeDto.ToString();
    }
}