using gAPI.Core.Attributes;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record UnsubscribeAckDto(
    UnsubscribeDto UnsubscribeDto,
    bool Cancelled,
    ExceptionDto? Exception)
{
    public override string ToString()
    {
        // This string is a key
        return UnsubscribeDto.ToString();
    }
}