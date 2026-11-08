using gAPI.Core.Ids;
using gAPI.Core.Server.Dtos;

namespace gAPI.Core.Server.Interfaces;

public interface IBaseBusConnection
{
    BusConnectionId BusConnectionId { get; }

    Task SendFireAndForgetAsync(BusRoutingDto routing, byte[] data, CancellationTokenSource cts);
    Task SendAsync(BusRoutingDto routing, byte[] data, CancellationTokenSource cts);
    IAsyncEnumerable<byte[]> InvokeAsync(BusRoutingDto routing, byte[] data, CancellationTokenSource cts);

    void RegisterAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationTokenSource cts);
    Task UnRegisterAsyncEnumerableArgument(BusRoutingDto routing, int argumentIndex);
}
