using gAPI.Fabric.Types;

namespace gAPI.Fabric.Models;

public record Message(
    ServiceId ServiceId,
    UserId? userId,
    ScopeId? scopeId,
    byte[] Data);
