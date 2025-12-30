namespace gAPI.Fabric.Types;

public readonly record struct SubscriptionId(
    //ServiceId ServiceId,
    UserId UserId,
    SessionId SessionId,
    FabricHostId ConnectionId
    );
