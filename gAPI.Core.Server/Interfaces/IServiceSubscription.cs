using gAPI.Core.Dtos;
using gAPI.Core.Ids;

namespace gAPI.Core.Server.Interfaces;

// Warning: This interface is only for escaping the gAPI core module to the application specific implementation
// Do not use this interface for mocking as it doesn't expose the full interface
public interface IServiceSubscription
{
    ServiceSubscriptionId Id { get; }
    ServiceId ServiceId { get; }
    SessionId SessionId { get; }
    UserId UserId { get; }

    IAsyncEnumerable<InvokeResponseDto> Send_InvokeRequest_ToClient_Async(InvokeRequestDto request, CancellationToken ct);
    Task<SendRequestDoneDto> Send_SendRequest_ToClient_Async(SendRequestDto message, CancellationToken ct);
    bool HasRequest(RequestId requestId);
    Task SendArgumentRequestAsync(InvokeArgumentRequestDto request, CancellationToken ct);
    Task SendArgumentResponseAsync(InvokeArgumentResponseDto response, CancellationToken ct);
}