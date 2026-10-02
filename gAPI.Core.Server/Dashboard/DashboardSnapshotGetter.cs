using gAPI.Core.Server.Collections;

namespace gAPI.Core.Server.Dashboard;

public class DashboardSnapshotGetter(
    ServerConnectionCollection Connections,
    ServiceSubscriptionCollection Services)
    : IDashboardSnapshotGetter
{
    public async Task DisconnectAllAsync()
    {
    }

    public FabricDashboardSnapshot GetDashboardSnapshot()
    {
        var connections = Connections.All
            .Select(connection => {
                var send = connection.GetSendBytesPerSecond();
                var receive = connection.GetReceiveBytesPerSecond();
                return new FabricConnectionSnapshot(
                    connection.Id,
                    send.bytes,
                    receive.bytes,
                    send.count,
                    receive.count);
            })
            .OrderBy(connection => connection.ConnectionId)
            .ToArray();

        var services = Services.All
            .Select(service =>
            {
                var send = service.GetReceiveBytesPerSecond();
                var received = service.GetSendBytesPerSecond();
                return new FabricServiceSnapshot(
                    service.ServiceId.ToString(),
                    send.bytes,
                    received.bytes,
                    send.count,
                    received.count,
                    0,
                    0);
            })
            .OrderBy(service => service.ServiceId, StringComparer.Ordinal)
            .ToArray();

        return new FabricDashboardSnapshot(DateTimeOffset.UtcNow, connections, services);
    }
}
