using gAPI.Types;

namespace gAPI.FabricNode.Models;

public record Subscription(
    SubscriptionId Id,
    FabricHost Connection);