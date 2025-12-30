using gAPI.Fabric.Types;

namespace gAPI.Fabric.Models;

public record SendMessage(
    ServiceId ServiceId,
    UserId? UserId,
    SessionId? SessionId,
    string Data);
