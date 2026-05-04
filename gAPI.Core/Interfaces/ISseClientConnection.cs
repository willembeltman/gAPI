using gAPI.Dtos;

namespace gAPI.Interfaces;

public interface IClientConnection : IDisposable
{
    Task MessageReceivedAsync(SendRequestDto message, CancellationToken ct);
    void SubscribeAsync(object implementation);
    void UnsubscribeAsync(object implementation);
}
