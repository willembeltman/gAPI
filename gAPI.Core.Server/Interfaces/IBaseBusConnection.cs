using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Server.Config;
using gAPI.Core.Server.Dtos;

namespace gAPI.Core.Server.Interfaces;

public interface IBaseBusConnection
{
    BusConnectionId BusConnectionId { get; }
    ServiceBusConfig Config { get; }

    void RegisterTimeout(BusRoutingDto routing, CancellationTokenSourceDispose cts);
    void UnRegisterTimeout(BusRoutingDto routing, CancellationTokenSourceDispose cts);

    void RegisterAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSourceDispose cts);
    Task UnRegisterAsyncEnumerableArgument(BusRoutingDto routing, int argumentIndex);

    Task SendFireAndForgetAsync(BusRoutingDto routing, byte[] data);
    Task SendAsync(BusRoutingDto routing, byte[] data, CancellationTokenSourceDispose cts);
    IAsyncEnumerable<byte[]> InvokeAsync(BusRoutingDto routing, byte[] data, CancellationTokenSourceDispose cts);
}
