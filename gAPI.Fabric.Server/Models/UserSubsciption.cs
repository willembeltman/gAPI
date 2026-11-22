using gAPI.Core.Ids;
using gAPI.Fabric.Server.Helpers;
using gAPI.Fabric.Server.Interfaces;
using gAPI.Fabric.Server.Services;
using System.Collections.Concurrent;

namespace gAPI.Fabric.Server.Models;

public class UserSubsciption(UserId Id) : PerformanceCounter, IActor
{
    public UserId Id { get; } = Id;
    public ConcurrentDictionary<FabricConnectionId, FabricHost> Connections { get; } = new();

    public void Subscribe(FabricHost connection) => Connections[connection.FabricConnectionId] = connection;
    public void Unsubscribe(FabricHost connection) => Connections.TryRemove(connection.FabricConnectionId, out _);

    public override string ToString() => Id.ToString();

    public void RemoveHost(FabricHost host)
    {
        Connections.TryRemove(host.FabricConnectionId, out _);
    }
}
