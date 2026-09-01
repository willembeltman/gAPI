using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record SendRequestDoneDto(
    RequestId RequestId,
    ServiceId ServiceId,
    ServiceMethodId MethodId,
    UserId? UserId,
    SessionId? SessionId,
    bool StateIsChanged,
    string? StateData,
    string? ExceptionMessage);