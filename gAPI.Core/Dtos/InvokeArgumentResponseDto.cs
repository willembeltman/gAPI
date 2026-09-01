using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record InvokeArgumentResponseDto(
    RequestId RequestId,
    int ArgumentIndex,
    Guid StreamId,
    bool IsCompleted,
    byte[] BinaryData);
