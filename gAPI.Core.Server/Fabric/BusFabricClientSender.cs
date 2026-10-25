using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Enums;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace gAPI.Core.Server.Fabric;

public class BusFabricClientSender(
    BusFabricClient fabricClient, ILoggerFactory loggerFactory)
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<FabricClientSender>();
    readonly Channel<Action<BinaryWriter>> SendQueue = Channel.CreateUnbounded<Action<BinaryWriter>>();

    public async Task SendKernel(CancellationToken ct)
    {
        await foreach (var item in SendQueue.Reader.ReadAllAsync(ct))
        {
            while (fabricClient.IsConnected == false || fabricClient.BinaryWriter == null)
            {
                await Task.Delay(10, ct);
            }
            try
            {
                var writer = fabricClient.BinaryWriter;
                if (writer != null)
                {
                    item(writer);
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error writing to Fabric stream");
            }
        }
    }

    private async Task EnqueueAsync(Action<BinaryWriter> write, CancellationToken ct)
    {
        try
        {
            await SendQueue.Writer.WriteAsync(write, ct);
        }
        catch (TaskCanceledException)
        {
        }
    }

    public async Task Send_BusSubscribe_ToFabricAsync(BusSubscribeDto subscribe, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusSubscribe);
            writer.Write(subscribe);
        }, ct);
    }
    public async Task Send_BusUnsubscribe_ToFabricAsync(BusUnsubscribeDto unsubscribe, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusUnsubscribe);
            writer.Write(unsubscribe);
        }, ct);
    }

    internal async Task Send_BusSendAndForgetRequest_ToFabricAsync(BusSendRequestDto request, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusFireAndForget);
            writer.Write(request);
        }, ct);
    }
    public async Task Send_BusSendAndForgetRequestCancelled_ToFabricAsync(BusSendRequestCancelledDto request, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusFireAndForgetCancelled);
            writer.Write(request);
        }, ct);
    }

    public async Task Send_BusSendRequest_ToFabricAsync(BusSendRequestDto request, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusSendRequest);
            writer.Write(request);
        }, ct);
    }
    public async Task Send_BusSendCancelled_ToFabricAsync(BusSendRequestCancelledDto request, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusSendRequestCancelled);
            writer.Write(request);
        }, ct);
    }
    public async Task Send_BusSendRequestDone_ToFabricAsync(BusSendRequestDoneDto ack, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusSendRequestDone);
            writer.Write(ack);
        }, ct);
    }

    internal async Task Send_BusInvokeRequest_ToFabricAsync(BusInvokeRequestDto request, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusInvokeRequest);
            writer.Write(request);
        }, ct);
    }
    public async Task Send_BusInvokeRequestCancelled_ToFabricAsync(BusInvokeRequestCancelledDto request, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusInvokeRequestCancelled);
            writer.Write(request);
        }, ct);
    }
    public async Task Send_BusInvokeRequestAck_ToFabricAsync(BusInvokeRequestAckDto ack, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusInvokeRequestAck);
            writer.Write(ack);
        }, ct);
    }
    public async Task Send_BusInvokeRequestDone_ToFabricAsync(BusInvokeRequestDoneDto done, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusInvokeRequestDone);
            writer.Write(done);
        }, ct);
    }

    public async Task Send_BusStreamingRequest_ToFabricAsync(BusStreamingRequestDto busStreamingRequestDto, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusStreamingRequest);
            writer.Write(busStreamingRequestDto);
        }, ct);
    }
    public async Task Send_BusStreamingResponse_ToFabricAsync(BusStreamingResponseDto response, CancellationToken ct)
    {
        await EnqueueAsync(writer =>
        {
            BusFabricConverter.WriteClientToHostMessageType(writer, BusFabricClientToHostMessageEnum.BusStreamingResponse);
            writer.Write(response);
        }, ct);
    }
}