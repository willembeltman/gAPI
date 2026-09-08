using gAPI.Core.Dtos;
using gAPI.Core.Ids;

namespace gAPI.Core.Server.Interfaces;

// Warning: This interface is only for escaping the gAPI core module to the application specific implementation
// Do not use this interface for mocking as it doesn't expose the full interface
public interface IServiceSubscription
{
    ClientConnectionId ClientConnectionId { get; }
    ServiceSubscriptionId ServiceSubscriptionId { get; }
    ServiceId ServiceId { get; }
    SessionId SessionId { get; }
    UserId UserId { get; }

    IAsyncEnumerable<byte[]> Send_InvokeRequest_ToClient_Async(InvokeRequestDto request, CancellationToken ct);
    Task Send_SendRequest_ToClient_Async(SendRequestDto message, CancellationToken ct);
    Task Send_StreamingRequest_ToClientAsync(StreamingRequestDto request, CancellationToken ct);
    Task Send_StreamingResponse_ToClientAsync(StreamingResponseDto response, CancellationToken ct);

    //bool HasRequest(RequestId requestId);
}