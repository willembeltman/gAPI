using gAPI.Core.Dtos;
using gAPI.Core.Interfaces;

namespace gAPI.Core.Server.Interfaces;

public interface IBaseFabricConnection : IBaseBusConnection
{
    Task ReceiveFireAndForgetAsync(BusSendRequestDto requestDto, CancellationToken ct);
    Task ReceiveSendRequestAsync(BusSendRequestDto requestDto, CancellationToken ct);
    IAsyncEnumerable<byte[]> ReceiveInvokeRequestAsync(BusInvokeRequestDto requestDto, CancellationToken ct);
    Task ReceiveStreamingRequestAsync(BusStreamingRequestDto requestDto, CancellationToken ct);
    Task ReceiveStreamingResponseAsync(BusStreamingResponseDto responseDto, CancellationToken ct);
}
