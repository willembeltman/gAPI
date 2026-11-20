using gAPI.Core.Dtos;

namespace gAPI.Core.Server.Interfaces;

public interface IServerConnection : ISpeedGetter
{
    string Id { get; }

    Task Send_FabricSendAndForgetRequest_ToClientAsync(SendRequestDto message, CancellationToken ct);

    Task Send_FabricSendRequest_ToClientAsync(SendRequestDto message, CancellationToken connectionLifecycleCt);
    Task Send_FabricSendRequestCancelled_ToClientAsync(SendRequestCancelledDto message, CancellationToken connectionLifecycleCt);

    Task Send_FabricInvokeRequest_ToClientAsync(InvokeRequestDto message, CancellationToken ct);
    Task Send_FabricInvokeRequestCancelled_ToClientAsync(InvokeRequestCancelledDto message, CancellationToken ct);

    Task Send_StreamingRequest_ToClientAsync(StreamingRequestDto message, CancellationToken ct);
    Task Send_StreamingResponse_ToClientAsync(StreamingResponseDto message, CancellationToken ct);

    Task Send_FabricStreamingRequest_ToClientAsync(StreamingRequestDto message, CancellationToken ct);
    Task Send_FabricStreamingResponse_ToClientAsync(StreamingResponseDto message, CancellationToken ct);

    Task Send_OnDisconnected_ToClientAsync(CancellationToken ct);
    Task Send_OnRestoreSubscriptions_ToClientAsync(CancellationToken ct);
    Task Send_SubscribeAck_ToClientAsync(SubscribeAckDto message, CancellationToken ct);
    Task Send_UnsubscribeAck_ToClientAsync(UnsubscribeAckDto message, CancellationToken ct);
}
