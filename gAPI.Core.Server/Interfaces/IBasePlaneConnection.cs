using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Server.Config;
using gAPI.Core.Server.Dtos;

namespace gAPI.Core.Server.Interfaces;

public interface IBasePlaneConnection
{
    PlaneConnectionId PlaneConnectionId { get; }
    BackPlaneConfig Config { get; }

    Task WaitForConnectionAsync(CancellationToken ct);

    void RegisterTimeout(PlaneRoutingDto routing, CancellationTokenSourceDispose cts);
    void UnRegisterTimeout(PlaneRoutingDto routing, CancellationTokenSourceDispose cts);

    void RegisterAsyncEnumerableArgument<T>(PlaneRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSourceDispose cts);
    Task UnRegisterAsyncEnumerableArgument(PlaneRoutingDto routing, int argumentIndex);

    Task SendFireAndForgetAsync(PlaneRoutingDto routing, byte[] data, CancellationToken ct);
    Task SendAsync(PlaneRoutingDto routing, byte[] data, CancellationTokenSourceDispose cts);
    IAsyncEnumerable<byte[]> InvokeAsync(PlaneRoutingDto routing, byte[] data, CancellationTokenSourceDispose cts, CancellationToken ct);
}
