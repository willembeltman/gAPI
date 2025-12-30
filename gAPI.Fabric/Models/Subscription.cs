using gAPI.Fabric.Types;

namespace gAPI.Fabric.Models;

public record Subscription(
    SubscriptionId Id,
    FabricHost Connection);