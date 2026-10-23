using gAPI.Core.Dtos;
using gAPI.Core.Interfaces;

namespace gAPI.Core.Server.Interfaces;

public interface IBaseFabricConnection : IBaseBusConnection
{
    Task Receive_BusFireAndForget_FromFabricAsync(BusSendRequestDto requestDto, CancellationToken ct);
    Task Receive_BusSendRequest_FromFabricAsync(BusSendRequestDto requestDto, CancellationToken ct);
    IAsyncEnumerable<byte[]> Receive_BusInvokeRequest_FromFabricAsync(BusInvokeRequestDto requestDto, CancellationToken ct);
    //Task ReceiveStreamingRequestAsync(BusStreamingRequestDto requestDto, CancellationToken ct);
    //Task ReceiveStreamingResponseAsync(BusStreamingResponseDto responseDto, CancellationToken ct);


    //Task ReceiveStreamingRequestAsync(BusStreamingRequestDto message, CancellationToken ct);
    //Task ReceiveStreamingResponseAsync(BusStreamingResponseDto message, CancellationToken ct);

    //Task Send_FabricSendAndForgetRequestCancelled_ToClientAsync(BusSendRequestCancelledDto message, CancellationToken ct);
    //Task Send_FabricSendAndForgetRequest_ToClientAsync(BusSendRequestDto message, CancellationToken ct);
    //Task Send_FabricSendRequest_ToClientAsync(BusSendRequestDto message, CancellationToken ct);
    //Task Send_FabricSendRequestCancelled_ToClientAsync(BusSendRequestCancelledDto message, CancellationToken ct);
    //Task Send_FabricInvokeRequest_ToClientAsync(BusInvokeRequestDto message, CancellationToken ct);
    //Task Send_FabricInvokeRequestCancelled_ToClientAsync(BusInvokeRequestCancelledDto message, CancellationToken ct);
}
