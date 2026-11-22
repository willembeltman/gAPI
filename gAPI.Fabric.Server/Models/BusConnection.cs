using gAPI.Core.Ids;
using gAPI.Fabric.Server.Helpers;
using gAPI.Fabric.Server.Interfaces;
using gAPI.Fabric.Server.Services;

namespace gAPI.Fabric.Server.Models;

public class BusConnection : PerformanceCounter, IActor
{
    public BusConnection(BusConnectionId busConnectionId, FabricBusHost connection)
    {
        BusConnectionId = busConnectionId;
        Connection = connection;
    }

    public BusConnectionId BusConnectionId { get; }
    public FabricBusHost Connection { get; }
}
