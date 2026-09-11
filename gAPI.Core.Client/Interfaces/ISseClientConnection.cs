using gAPI.Core.Dtos;

namespace gAPI.Core.Client.Interfaces;

public interface ISseClientConnection : IDisposable
{
    Task SendRequestCancelled_ReceivedAsync(SendRequestCancelledDto sendRequestCancelled, CancellationToken token);
    Task SendRequest_ReceivedAsync(SendRequestDto sendRequest, CancellationToken ct);
    void SubscribeAsync(object implementation);
    void UnsubscribeAsync(object implementation);
}
