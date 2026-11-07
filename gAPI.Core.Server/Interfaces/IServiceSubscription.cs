using gAPI.Core.Dtos;
using gAPI.Core.Ids;

namespace gAPI.Core.Server.Interfaces;

public interface IServiceSubscription : ISpeedGetter
{
    IServerConnection Connection { get; }
    ClientConnectionId ClientConnectionId { get; }
    ServiceSubscriptionId ServiceSubscriptionId { get; }
    ServiceId ServiceId { get; }
    SessionId SessionId { get; }
    UserId UserId { get; }
}