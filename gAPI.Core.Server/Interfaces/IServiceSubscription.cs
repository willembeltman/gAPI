using gAPI.Core.Ids;

namespace gAPI.Core.Server.Interfaces;

public interface IServiceSubscription : ISpeedGetter
{
    ServiceSubscriptionId Id { get; }
    IServerConnection Connection { get; }
    ClientConnectionId ClientConnectionId { get; } // SSE = 1 verbinding per client, per service, WSS = 1 verbinding per client, meerdere services 
    SessionId SessionId { get; }
    ServiceId ServiceId { get; }
    UserId? UserId { get; }
    bool IsAuthenticated { get; }
}