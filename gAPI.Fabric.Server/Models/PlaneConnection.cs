using gAPI.Core.Ids;
using gAPI.Fabric.Server.Helpers;
using gAPI.Fabric.Server.Interfaces;
using gAPI.Fabric.Server.Services;

namespace gAPI.Fabric.Server.Models;

public class PlaneConnection : PerformanceCounter, IActor
{
    public PlaneConnection(PlaneConnectionId planeConnectionId, FabricPlaneHost connection)
    {
        PlaneConnectionId = planeConnectionId;
        Connection = connection;
    }

    public PlaneConnectionId PlaneConnectionId { get; }
    public FabricPlaneHost Connection { get; }

}
