using gAPI.Core.Client.Config;
using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;

namespace gAPI.Core.Client.Interfaces;

public interface IWssClientConnection : IClientLoggerFactory
{
    SessionId SessionId { get; }
    bool Initialized { get; }
    bool IsConnected { get; }
    FabricManagerId FabricManagerId { get; }
    FabricConnectionId FabricConnectionId { get; }
    ClientConnectionId ClientConnectionId { get; }
    ClientConfig Config { get; }

    void Start(CancellationToken ct);

    void RegisterTimeout(RoutingDto routing, CancellationTokenSourceDispose cts);
    void UnRegisterTimeout(RoutingDto routing, CancellationTokenSourceDispose cts);

    Task Send_FireAndForget_ToServerAsync(RoutingDto routing, byte[] data, CancellationTokenSourceDispose ct);
    Task Send_SendRequest_ToServerAsync(RoutingDto routing, byte[] data, CancellationTokenSourceDispose ct);
    IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServerAsync(RoutingDto routing, byte[] data, CancellationTokenSourceDispose ct);
    
    void RegisterAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationToken cancellationToken);
    Task UnRegisterAsyncEnumerableArgument(RoutingDto routing, int argumentIndex);
}