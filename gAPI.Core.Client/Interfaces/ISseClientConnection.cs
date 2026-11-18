using gAPI.Core.Ids;

namespace gAPI.Core.Client.Interfaces;

public interface ISseClientConnection : IDisposable
{
    bool Initialized { get; }

    void Start(CancellationToken ct);

    Task SubscribeAsync(object implementation, CancellationToken ct = default);
    Task UnsubscribeAsync(object implementation, CancellationToken ct = default);

    Task Send_SendRequest_ToServiceAsync(
        ServiceId serviceId,
        ServiceMethodId methodId,
        byte[] data,
        CancellationToken ct);
    IAsyncEnumerable<byte[]> Send_InvokeRequest_ToServiceAsync(
        ServiceId serviceId,
        ServiceMethodId methodId,
        byte[] data,
        CancellationToken ct);

}
