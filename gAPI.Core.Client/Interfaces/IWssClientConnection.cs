using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;

namespace gAPI.Core.Client.Interfaces;

public interface IWssClientConnection : IClientLoggerFactory
{
    SessionId SessionId { get; }
    bool Initialized { get; }
    bool IsConnected { get; }

    Task TryConnectAsync(CancellationToken ct);

    Task Send_SendRequest_ToServerAsync(RoutingDto routing, byte[] data, CancellationToken ct);
    IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServerAsync(RoutingDto routing, byte[] data, CancellationToken ct);
    void RegisterAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationToken cancellationToken);
    void UnRegisterAsyncEnumerableArguments(RoutingDto routing);
}