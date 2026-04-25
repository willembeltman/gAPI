using gAPI.Dtos;

namespace gAPI.Interfaces;

public interface ISseClientConnection : IAsyncDisposable
{
    bool Initialized { get; }
    Task MessageReceivedAsync(SendRequestDto message, CancellationToken ct);

    Task SubscribeAsync(object implementation);
    Task UnsubscribeAsync(object implementation);
}
