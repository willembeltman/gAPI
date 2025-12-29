namespace gAPI.Fabric;

public readonly record struct SubscriberId(
    ServiceId ServiceId,
    UserId UserId,
    ScopeId ScopeId,
    ConnectionId ClientId
    );
