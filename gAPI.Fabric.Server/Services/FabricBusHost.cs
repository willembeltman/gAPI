using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Dashboard;
using gAPI.Core.Server.Dtos;
using gAPI.Core.Server.Enums;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Server.Interfaces;
using gAPI.Fabric.Server.Collections;
using gAPI.Fabric.Server.Helpers;
using gAPI.Fabric.Server.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Threading.Channels;

namespace gAPI.Fabric.Server.Services;

public sealed class FabricBusHost : PerformanceCounter, IActor
{
    private readonly FabricManager Manager;
    private readonly TcpClient TcpClient;
    private readonly NetworkStream Stream;
    private readonly Channel<SendQueueItem> SendQueue;
    private readonly CancellationTokenSource Cts;

    public FabricBusConnectionId FabricBusConnectionId { get; }
    public ILogger<FabricHost> Logger { get; }

    private FabricBusHostCollection BusConnections => Manager.AllBusConnections;

    public FabricBusHost(
        ILoggerFactory loggerFactory,
        FabricManager manager,
        TcpClient tcpClient)
    {
        Manager = manager;
        TcpClient = tcpClient;
        Cts = new CancellationTokenSource();
        Stream = tcpClient.GetStream();
        SendQueue = Channel.CreateUnbounded<SendQueueItem>();
        FabricBusConnectionId = BusConnections.AddConnection(this);
        Logger = loggerFactory.CreateLogger<FabricHost>();
    }

    public void Start()
    {
        _ = Task.Run(ReceiveLoop);
        _ = Task.Run(SendLoop);
    }

    private async Task Send_SynchronizeFabricIds_ToApiAsync(SynchronizeBusFabricIdsDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_SynchronizeFabricIds_ToApiAsync({ids}, {actor})", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            BusFabricConverter.WriteHostToClientMessageType(writer, BusFabricHostToClientMessageEnum.SynchronizeFabricIds);
            writer.Write(message);
        }, actor);
    }

    public async Task Send_BusSubscribeAck_ToApiAsync(BusSubscribeAckDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            BusFabricConverter.WriteHostToClientMessageType(writer, BusFabricHostToClientMessageEnum.SubscribeAck);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_BusUnsubscribeAck_ToApiAsync(BusUnsubscribeAckDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            BusFabricConverter.WriteHostToClientMessageType(writer, BusFabricHostToClientMessageEnum.UnsubscribeAck);
            writer.Write(message);
        }, actor);
    }

    public async Task Send_BusFireAndForget_ToApiAsync(BusSendRequestDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            BusFabricConverter.WriteHostToClientMessageType(writer, BusFabricHostToClientMessageEnum.BusFireAndForget);
            writer.Write(message);
        }, actor);
    }

    public async Task Send_BusSendRequest_ToApiAsync(BusSendRequestDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            BusFabricConverter.WriteHostToClientMessageType(writer, BusFabricHostToClientMessageEnum.BusSendRequest);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_BusSendRequestCancelled_ToApiAsync(BusSendRequestCancelledDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            BusFabricConverter.WriteHostToClientMessageType(writer, BusFabricHostToClientMessageEnum.BusSendRequestCancelled);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_BusSendRequestDone_ToApiAsync(BusSendRequestDoneDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            BusFabricConverter.WriteHostToClientMessageType(writer, BusFabricHostToClientMessageEnum.BusSendRequestDone);
            writer.Write(message);
        }, actor);
    }

    public async Task Send_BusInvokeRequest_ToApiAsync(BusInvokeRequestDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            BusFabricConverter.WriteHostToClientMessageType(writer, BusFabricHostToClientMessageEnum.BusInvokeRequest);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_BusInvokeRequestCancelled_ToApiAsync(BusInvokeRequestCancelledDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            BusFabricConverter.WriteHostToClientMessageType(writer, BusFabricHostToClientMessageEnum.BusInvokeRequestCancelled);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_BusInvokeRequestAck_ToApiAsync(BusInvokeRequestAckDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            BusFabricConverter.WriteHostToClientMessageType(writer, BusFabricHostToClientMessageEnum.BusInvokeRequestAck);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_BusInvokeRequestDone_ToApiAsync(BusInvokeRequestDoneDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            BusFabricConverter.WriteHostToClientMessageType(writer, BusFabricHostToClientMessageEnum.BusInvokeRequestDone);
            writer.Write(message);
        }, actor);
    }

    public async Task Send_BusStreamingRequest_ToApiAsync(BusStreamingRequestDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_BusStreamingRequest_ToApiAsync({request}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            BusFabricConverter.WriteHostToClientMessageType(writer, BusFabricHostToClientMessageEnum.BusStreamingRequest);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_BusStreamingResponse_ToApiAsync(BusStreamingResponseDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_BusStreamingResponse_ToApiAsync({response}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            BusFabricConverter.WriteHostToClientMessageType(writer, BusFabricHostToClientMessageEnum.BusStreamingResponse);
            writer.Write(message);
        }, actor);
    }


    private async Task SendLoop()
    {
        using var counter = new CountingDuplexStream(Stream);
        using var writer = new BinaryWriter(counter);

        await Send_SynchronizeFabricIds_ToApiAsync(new SynchronizeBusFabricIdsDto(
            Manager.FabricManagerId,
            FabricBusConnectionId), Manager.System);

        var previous = counter.BytesWritten;
        await foreach (var item in SendQueue.Reader.ReadAllAsync(Cts.Token))
        {
            item.write(writer);
            writer.Flush();
            if (Cts.IsCancellationRequested) break;

            var size = counter.BytesWritten - previous;
            previous = counter.BytesWritten;
            item.actor.EnqueueSend(size);
            EnqueueSend(size);
        }
        Dispose();
    }
    private async Task Enqueue(Action<BinaryWriter> write, IActor actor)
    {
        await SendQueue.Writer.WriteAsync(new(write, actor));
    }

    private async Task ReceiveLoop()
    {
        Logger.LogInformation($"FabricHost {FabricBusConnectionId} started");

        try
        {
            using var counter = new CountingDuplexStream(Stream);
            using var reader = new BinaryReader(counter);
            var previousBytesRead = counter.BytesRead;
            while (!Cts.IsCancellationRequested)
            {
                var messageType = BusFabricConverter.ReadClientToHostMessageType(reader);
                //if (Logger.IsEnabled(LogLevel.Trace))
                //    Logger.LogTrace("{now}: ReceiveLoop({messageType})", DateTime.Now.ToString("HH:mm:ss.fff"), messageType);
                switch (messageType)
                {
                    case BusFabricClientToHostMessageEnum.BusSubscribe:
                        {
                            var busSubscribe = reader.ReadBusSubscribeDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusSubscribe_FromApiAsync(this, busSubscribe, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusUnsubscribe:
                        {
                            var busUnsubscribe = reader.ReadBusUnsubscribeDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusUnsubscribe_FromApiAsync(this, busUnsubscribe, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusFireAndForget:
                        {
                            var busFireAndForget = reader.ReadBusSendRequestDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusSendFireAndForget_FromApiAsync(this, busFireAndForget, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusSendRequest:
                        {
                            var busSendRequest = reader.ReadBusSendRequestDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusSendRequest_FromApiAsync(this, busSendRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusSendRequestCancelled:
                        {
                            var busSendRequestCancelled = reader.ReadBusSendRequestCancelledDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusSendRequestCancelled_FromApiAsync(this, busSendRequestCancelled, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusSendRequestDone:
                        {
                            var busSendRequestAck = reader.ReadBusSendRequestDoneDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusSendRequestDone_FromApiAsync(this, busSendRequestAck, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusInvokeRequest:
                        {
                            var busInvokeRequest = reader.ReadBusInvokeRequestDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusInvokeRequest_FromApiAsync(this, busInvokeRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusInvokeRequestCancelled:
                        {
                            var busInvokeRequest = reader.ReadBusInvokeRequestCancelledDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusInvokeRequestCancelled_FromApiAsync(this, busInvokeRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusInvokeRequestAck:
                        {
                            var busInvokeRequestAck = reader.ReadBusInvokeRequestAckDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusInvokeRequestAck_FromApiAsync(this, busInvokeRequestAck, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusInvokeRequestDone:
                        {
                            var busInvokeRequestDone = reader.ReadBusInvokeRequestDoneDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusInvokeRequestDone_FromApiAsync(this, busInvokeRequestDone, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusStreamingRequest:
                        {
                            var busStreamingRequest = reader.ReadBusStreamingRequestDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusStreamingRequest_FromApiAsync(this, busStreamingRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusStreamingResponse:
                        {
                            var busStreamingResponse = reader.ReadBusStreamingResponseDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusStreamingResponse_FromApiAsync(this, busStreamingResponse, receiveSize, Cts.Token);
                        }
                        break;
                }

                var size2 = counter.BytesRead - previousBytesRead;
                previousBytesRead = counter.BytesRead;
                EnqueueReceive(size2);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "FabricClient #{FabricBusConnectionId}: receive loop failed.", FabricBusConnectionId.Value);
        }
        Dispose();

        Logger.LogInformation("FabricHost {FabricBusConnectionId} stopped", FabricBusConnectionId);
    }

    public void Dispose()
    {
        Cts.Dispose();
        Stream.Dispose();
        TcpClient.Dispose();
        Manager.Disconnected(this);
    }
}
