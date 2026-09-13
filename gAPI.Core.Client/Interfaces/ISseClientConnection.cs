using gAPI.Core.Dtos;

namespace gAPI.Core.Client.Interfaces;

public interface ISseClientConnection : IDisposable
{
    bool Initialized { get; }
    Task SendRequestCancelled_ReceivedAsync(SendRequestCancelledClientDto sendRequestCancelled, CancellationToken token);
    Task SendRequest_ReceivedAsync(SendRequestClientDto sendRequest, CancellationToken ct);
    Task SubscribeAsync(object implementation);
    Task UnsubscribeAsync(object implementation);
}
