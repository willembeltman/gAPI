using gAPI.Dtos;
using gAPI.Ids;

namespace gAPI.Interfaces;

// Warning: This interface is only for escaping the gAPI core module to the application specific implementation
// Do not use this interface for mocking as it doesn't expose the full interface
public interface ISseHost
{
    SseHostId Id { get; }
    ServiceId ServiceId { get; }
    SessionId SessionId { get; }
    UserId UserId { get; }

    IAsyncEnumerable<InvokeResponseDto> InvokeAsync(InvokeRequestDto request, CancellationToken ct);
    Task SendAsync(SendRequestDto message, CancellationToken ct);
}