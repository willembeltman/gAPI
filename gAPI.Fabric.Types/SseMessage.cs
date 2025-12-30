namespace gAPI.Fabric.Types;

public record SseMessage(
    ServiceId ServiceId,
    UserId? UserId,
    SessionId? SessionId,
    string Data);
