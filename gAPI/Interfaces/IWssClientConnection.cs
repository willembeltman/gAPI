using gAPI.Dtos;

namespace gAPI.Interfaces;

public interface IWssClientConnection : IWssLoggerFactory
{
    bool Initialized { get; }
    bool IsConnected { get; }

    Task Send_Subscribe_ToServerAsync(SubscribeDto subscribe, CancellationToken ct);
    Task Send_Unsubscribe_ToServerAsync(UnsubscribeDto unsubscribe, CancellationToken ct);
    Task TryConnectAsync(CancellationToken ct);
    Task ForceReconnectAsync(CancellationToken ct);

    Task Send_SendRequest_ToServerAsync(ApiSendRequestDto sendRequest, CancellationToken ct);
    Task Send_InvokeRequest_ToServerAsync(ApiInvokeRequestDto invokeRequest, CancellationToken ct);
    Task Send_InvokeResponse_ToServerAsync(InvokeResponseDto invokeResponse, CancellationToken ct);
    Task Send_InvokeResponseDone_ToServerAsync(InvokeResponseDoneDto invokeResponseDone, CancellationToken ct);

}