using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record UnsubscribeAckDto(
    UnsubscribeDto UnsubscribeDto,
    bool Cancelled,
    string? ExceptionMessage)
{
    public override string ToString()
    {
        // This string is a key
        return UnsubscribeDto.ToString();
    }
}