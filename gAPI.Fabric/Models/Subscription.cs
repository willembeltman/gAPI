using gAPI.Types;

namespace gAPI.Fabric.Models;

public record Subscription(
    SubscriptionId Id,
    FabricHost Connection);