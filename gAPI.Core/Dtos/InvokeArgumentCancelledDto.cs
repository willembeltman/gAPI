using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record InvokeArgumentCancelledDto(
    RequestId RequestId,
    int ArgumentIndex,
    Guid StreamId,
    string? Reason);
