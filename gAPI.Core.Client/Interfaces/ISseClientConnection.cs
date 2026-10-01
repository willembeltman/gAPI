using gAPI.Core.Dtos;
using gAPI.Core.Ids;

namespace gAPI.Core.Client.Interfaces;

public interface ISseClientConnection : IDisposable
{
    bool Initialized { get; }

    Task SubscribeAsync(object implementation, CancellationToken ct = default);
    Task UnsubscribeAsync(object implementation, CancellationToken ct = default);

    Task SendRequest_ReceivedAsync(
        ServiceId serviceId,
        ServiceMethodId methodId,
        byte[] data,
        CancellationToken ct);
    IAsyncEnumerable<byte[]> InvokeRequest_ReceivedAsync(
        ServiceId serviceId,
        ServiceMethodId methodId,
        byte[] data, 
        CancellationToken ct);

}
