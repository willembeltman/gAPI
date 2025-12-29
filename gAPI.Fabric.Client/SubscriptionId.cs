using gAPI.Fabric.Types;

namespace gAPI.Fabric.Client;

public readonly record struct SubscriptionId(
    ServiceId ServiceId,
    UserId UserId,
    ScopeId ScopeId,
    ConnectionId ClientId
    );
