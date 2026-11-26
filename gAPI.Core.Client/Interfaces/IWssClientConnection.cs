using gAPI.Core.Client.Config;
using gAPI.Core.Client.Logs;
using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;

namespace gAPI.Core.Client.Interfaces;

public interface IWssClientConnection
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

    Task Send_FireAndForget_ToServerAsync(RoutingDto routing, byte[] data, CancellationToken ct);
    Task Send_SendRequest_ToServerAsync(RoutingDto routing, byte[] data, CancellationTokenSourceDispose cts);
    IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServerAsync(RoutingDto routing, byte[] data, CancellationTokenSourceDispose cts, CancellationToken ct);
    
    void RegisterAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSourceDispose timoutCts);
    Task UnRegisterAsyncEnumerableArgument(RoutingDto routing, int argumentIndex);

    Task WaitForConnectionAsync(CancellationToken ct);
    Task<bool> CheckConnectionAsync(CancellationToken ct = default);
}