using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Core.Dtos;

[GenerateSerializer]
public record SendRequestCancelledDto(
    RequestId RequestId,
    ServiceId ServiceId,
    ServiceMethodId MethodId,
    UserId? UserId,
    SessionId? SessionId,
    string? Reason);
