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
            .Select(connection =>
            {
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
            .GroupBy(a => a.ServiceId)
            .Select(service =>
            {
                var send = service
                    .Select(a => a.GetReceiveBytesPerSecond());
                var sendBytes = send.Sum(a => a.bytes);
                var sendPackages = send.Sum(a => a.count);

                var received = service
                    .Select(a => a.GetSendBytesPerSecond());
                var receivedBytes = send.Sum(a => a.bytes);
                var receivedPackages = send.Sum(a => a.count);

                var sessions = service.GroupBy(a => a.SessionId).Count();
                var users = service.GroupBy(a => a.UserId).Count();

                //var received = service.GetSendBytesPerSecond();
                return new FabricServiceSnapshot(
                    service.Key.ToString(),
                    sendBytes,
                    receivedBytes,
                    sendPackages,
                    receivedPackages,
                    sessions,
                    users);
            })
            .OrderBy(service => service.ServiceId, StringComparer.Ordinal)
            .ToArray();

        return new FabricDashboardSnapshot(DateTimeOffset.UtcNow, connections, services);
    }
}
