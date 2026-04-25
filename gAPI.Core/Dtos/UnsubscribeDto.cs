using gAPI.Attributes;
using gAPI.Ids;
using gAPI.Interfaces;

namespace gAPI.Dtos;

[GenerateSerializer]
public class UnsubscribeDto
{
    public ServiceId ServiceId { get; set; } = default!;
    public UserId UserId { get; set; } = default!;
    public SessionId SessionId { get; set; } = default!;

    public override string ToString()
    {
        // This string is a key
        return $"{ServiceId} #{SessionId} ({UserId})";
    }
}