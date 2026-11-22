using gAPI.Core.Ids;
using gAPI.Fabric.Server.Helpers;
using gAPI.Fabric.Server.Interfaces;
using gAPI.Fabric.Server.Services;

namespace gAPI.Fabric.Server.Models;

public class PlaneServiceSubscription : PerformanceCounter, IActor
{
    public PlaneServiceSubscription(ServiceSubscriptionId serviceSubscriptionId, FabricPlaneHost connection)
    {
        ServiceSubscriptionId = serviceSubscriptionId;
        Connection = connection;
    }

    public ServiceSubscriptionId ServiceSubscriptionId { get; }
    public FabricPlaneHost Connection { get; }
}
