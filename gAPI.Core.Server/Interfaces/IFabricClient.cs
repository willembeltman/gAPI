using gAPI.Core.Dtos;
using gAPI.Core.Helpers;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;

namespace gAPI.Core.Server.Interfaces;

public interface IFabricClient : IAsyncDisposable
{
    FabricConnectionId FabricConnectionId { get; }
    FabricManagerId FabricManagerId { get; }
    bool IsConnected { get; }

    Task ClearSession(SessionId sessionId, CancellationToken ct);
    Task ConnectAsync();
    Task DisconnectAsync();
    Task<string?> GetSessionCookieData(string sessionIdString, CancellationToken ct);
    IAsyncEnumerable<byte[]> InvokeAsync(RoutingDto routing, byte[] data, CancellationToken ct);
    Task ReceiveKernel(CancellationToken ct);
    Task ReconnectAsync(CancellationToken ct);
    void RegisterAsyncEnumerableArgument<T>(RoutingDto routing, int argumentIndex, IAsyncEnumerable<T> source, Func<T, byte[]> serializer, CancellationToken cancellationToken);
    void RegisterAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex, IAsyncEnumerable<byte[]> source, CancellationToken cancellationToken);
    RemoteAsyncEnumerable<byte[]> RegisterRemoteAsyncEnumerableArgumentByte(RoutingDto routing, int argumentIndex);
    Task SendAsync(RoutingDto routing, byte[] data, CancellationToken ct);
    Task SendFireAndForgetAsync(RoutingDto routing, byte[] data, CancellationToken ct);
    Task Send_FabricInvokeRequestAck_ToFabricAsync(InvokeRequestAckDto message, CancellationToken ct);
    Task Send_FabricSendRequestDone_ToFabricAsync(SendRequestDoneDto done, CancellationToken ct);
    Task Send_StreamingRequestClientToServer_ToFabricAsync(StreamingRequestDto message, CancellationToken ct);
    Task Send_StreamingResponseClientToServer_ToFabricAsync(StreamingResponseDto message, CancellationToken ct);
    Task SubscribeAsync(IServiceSubscription serviceSubscription, CancellationToken ct);
    Task UnRegisterAsyncEnumerableArgument(RoutingDto routing, int argumentIndex);
    Task UnsubscribeAsync(IServiceSubscription serviceSubscription, CancellationToken ct);
    Task UpdateSession(SessionId sessionId, string? cookieData, CancellationToken ct);

    Task BusSendFireAndForgetAsync(IBaseFabricConnection baseBusConnection, BusSendRequestDto request, CancellationTokenSource cts);
    Task BusSendAsync(IBaseFabricConnection baseBusConnection, BusSendRequestDto request, CancellationTokenSource cts);
    IAsyncEnumerable<byte[]> BusInvokeAsync(IBaseFabricConnection baseBusConnection, BusInvokeRequestDto request, CancellationTokenSource cts);
    
    Task Send_BusStreamingRequest_ToFabricAsync(IBaseFabricConnection baseBusConnection, BusStreamingRequestDto request, CancellationToken ct);
    Task Send_BusStreamingResponse_ToFabricAsync(IBaseFabricConnection baseBusConnection, BusStreamingResponseDto response, CancellationToken ct);
    Task BusSubscribeAsync(IBaseFabricConnection baseBusConnection, BusSubscribeDto subscribe, CancellationToken ct);
    Task BusUnsubscribeAsync(IBaseFabricConnection baseBusConnection, BusUnsubscribeDto unsubscribe, CancellationToken ct);
    Task Send_FabricInvokeRequestDone_ToFabricAsync(InvokeRequestDoneDto message, CancellationToken ct);
}