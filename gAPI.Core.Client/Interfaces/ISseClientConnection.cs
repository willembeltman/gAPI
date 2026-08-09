using gAPI.Core.Dtos;

namespace gAPI.Core.Client.Interfaces;

public interface ISseClientConnection : IDisposable
{
    Task MessageReceivedAsync(SendRequestDto message, CancellationToken ct);
    void SubscribeAsync(object implementation);
    void UnsubscribeAsync(object implementation);
}
