using gAPI.Core.Dtos;
using gAPI.Core.Ids;

namespace gAPI.Core.ServiceBus.Interfaces;

public interface IBaseBusConnection
{
    BusConnectionId BusConnectionId { get; }

    Task SendFireAndForgetAsync(BusRoutingDto routing, byte[] data, CancellationToken ct);
    Task SendAsync(BusRoutingDto routing, byte[] data, CancellationToken ct);
    IAsyncEnumerable<byte[]> InvokeAsync(BusRoutingDto routing, byte[] data, CancellationToken ct);

    void RegisterAsyncEnumerableArgument<T>(BusRoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationToken cancellationToken);
    Task UnRegisterAsyncEnumerableArgument(BusRoutingDto routing, int argumentIndex);
}
