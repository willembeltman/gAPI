using gAPI.Fabric;

namespace gAPI.Types
{
    public readonly struct SubscriptionId
    {
        public SubscriptionId(
            UserId userId,
            SessionId sessionId,
            FabricHostId connectionId
        )
        {
            UserId = userId;
            SessionId = sessionId;
            ConnectionId = connectionId;
        }

        public UserId UserId { get; }
        public SessionId SessionId { get; }
        public FabricHostId ConnectionId { get; }
    }
}