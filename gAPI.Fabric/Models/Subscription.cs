using gAPI.Types;

namespace gAPI.FabricClient.Models;

public record Subscription(
    SubscriptionId Id,
    FabricHost Connection);