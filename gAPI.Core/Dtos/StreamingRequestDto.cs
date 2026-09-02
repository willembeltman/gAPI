using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record StreamingRequestDto(
    RequestId RequestId,
    int ArgumentIndex,
    Guid StreamId);
