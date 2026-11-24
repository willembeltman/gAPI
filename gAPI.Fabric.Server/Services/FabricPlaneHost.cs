using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Server.Dashboard;
using gAPI.Core.Server.Dtos;
using gAPI.Core.Server.Enums;
using gAPI.Core.Server.Fabric;
using gAPI.Fabric.Server.Collections;
using gAPI.Fabric.Server.Helpers;
using gAPI.Fabric.Server.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Channels;

namespace gAPI.Fabric.Server.Services;

public sealed class FabricPlaneHost : IActor
{
    private readonly FabricManager Manager;
    private readonly TcpClient TcpClient;
    private readonly NetworkStream Stream;
    private readonly Channel<SendQueueItem> SendQueue;
    private readonly CancellationTokenSource Cts;

    public FabricPlaneConnectionId FabricPlaneConnectionId { get; }
    public ILogger<FabricPlaneHost> Logger { get; }
    public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();

    private readonly ConcurrentQueue<(double time, long bytes)> SendBytesLogger = new();
    private readonly ConcurrentQueue<(double time, long bytes)> ReceivedBytesLogger = new();

    private (int count, long bytes) GetSpeed(ConcurrentQueue<(double time, long bytes)> queue)
    {
        var interval = 1.0;
        var now = Stopwatch.Elapsed.TotalSeconds;

        // Verwijder oude entries
        while (queue.TryPeek(out var entry) && entry.time < now - interval)
            queue.TryDequeue(out _);

        var bytes = 0L;
        var count = 0;
        foreach (var item in queue)
        {
            bytes += item.bytes;
            count++;
        }
        return new((int)(count / interval), (int)(bytes / interval));
    }
    public (int count, long bytes) GetSendBytesPerSecond() => GetSpeed(SendBytesLogger);
    public (int count, long bytes) GetReceiveBytesPerSecond() => GetSpeed(ReceivedBytesLogger);
    public void EnqueueSend(long size)
    {
        SendBytesLogger.Enqueue((Stopwatch.Elapsed.TotalSeconds, size));
    }
    public void EnqueueReceive(long size)
    {
        ReceivedBytesLogger.Enqueue((Stopwatch.Elapsed.TotalSeconds, size));
    }

    private FabricPlaneHostCollection PlaneConnections => Manager.AllPlaneConnections;

    public LogLevel MinimumLevel => Manager.Config.LogLevel;

    public FabricPlaneHost(
        ILoggerFactory loggerFactory,
        FabricManager manager,
        TcpClient tcpClient)
    {
        Manager = manager;
        TcpClient = tcpClient;
        Cts = new CancellationTokenSource();
        Stream = tcpClient.GetStream();
        SendQueue = Channel.CreateUnbounded<SendQueueItem>();
        FabricPlaneConnectionId = PlaneConnections.AddConnection(this);
        Logger = loggerFactory.CreateLogger<FabricPlaneHost>();
    }

    public void Start()
    {
        _ = Task.Run(ReceiveLoop);
        _ = Task.Run(SendLoop);
    }

    private async Task Send_SynchronizeFabricIds_ToApiAsync(SynchronizePlaneFabricIdsDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_SynchronizeFabricIds_ToApiAsync({ids}, {actor})", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            PlaneFabricConverter.WriteHostToClientMessageType(writer, PlaneFabricHostToClientMessageEnum.SynchronizeFabricIds);
            writer.Write(message);
        }, actor);
    }

    public async Task Send_PlaneSubscribeAck_ToApiAsync(PlaneSubscribeAckDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            PlaneFabricConverter.WriteHostToClientMessageType(writer, PlaneFabricHostToClientMessageEnum.SubscribeAck);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_PlaneUnsubscribeAck_ToApiAsync(PlaneUnsubscribeAckDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            PlaneFabricConverter.WriteHostToClientMessageType(writer, PlaneFabricHostToClientMessageEnum.UnsubscribeAck);
            writer.Write(message);
        }, actor);
    }

    public async Task Send_PlaneFireAndForget_ToApiAsync(PlaneSendRequestDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            PlaneFabricConverter.WriteHostToClientMessageType(writer, PlaneFabricHostToClientMessageEnum.PlaneFireAndForget);
            writer.Write(message);
        }, actor);
    }

    public async Task Send_PlaneSendRequest_ToApiAsync(PlaneSendRequestDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            PlaneFabricConverter.WriteHostToClientMessageType(writer, PlaneFabricHostToClientMessageEnum.PlaneSendRequest);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_PlaneSendRequestCancelled_ToApiAsync(PlaneSendRequestCancelledDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            PlaneFabricConverter.WriteHostToClientMessageType(writer, PlaneFabricHostToClientMessageEnum.PlaneSendRequestCancelled);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_PlaneSendRequestDone_ToApiAsync(PlaneSendRequestDoneDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            PlaneFabricConverter.WriteHostToClientMessageType(writer, PlaneFabricHostToClientMessageEnum.PlaneSendRequestDone);
            writer.Write(message);
        }, actor);
    }

    public async Task Send_PlaneInvokeRequest_ToApiAsync(PlaneInvokeRequestDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            PlaneFabricConverter.WriteHostToClientMessageType(writer, PlaneFabricHostToClientMessageEnum.PlaneInvokeRequest);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_PlaneInvokeRequestCancelled_ToApiAsync(PlaneInvokeRequestCancelledDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            PlaneFabricConverter.WriteHostToClientMessageType(writer, PlaneFabricHostToClientMessageEnum.PlaneInvokeRequestCancelled);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_PlaneInvokeRequestAck_ToApiAsync(PlaneInvokeRequestAckDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            PlaneFabricConverter.WriteHostToClientMessageType(writer, PlaneFabricHostToClientMessageEnum.PlaneInvokeRequestAck);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_PlaneInvokeRequestDone_ToApiAsync(PlaneInvokeRequestDoneDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            PlaneFabricConverter.WriteHostToClientMessageType(writer, PlaneFabricHostToClientMessageEnum.PlaneInvokeRequestDone);
            writer.Write(message);
        }, actor);
    }

    public async Task Send_PlaneStreamingRequest_ToApiAsync(PlaneStreamingRequestDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_PlaneStreamingRequest_ToApiAsync({request}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            PlaneFabricConverter.WriteHostToClientMessageType(writer, PlaneFabricHostToClientMessageEnum.PlaneStreamingRequest);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_PlaneStreamingResponse_ToApiAsync(PlaneStreamingResponseDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_PlaneStreamingResponse_ToApiAsync({response}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            PlaneFabricConverter.WriteHostToClientMessageType(writer, PlaneFabricHostToClientMessageEnum.PlaneStreamingResponse);
            writer.Write(message);
        }, actor);
    }


    private async Task SendLoop()
    {
        using var counter = new CountingDuplexStream(Stream);
        using var writer = new BinaryWriter(counter);

        await Send_SynchronizeFabricIds_ToApiAsync(new SynchronizePlaneFabricIdsDto(
            Manager.FabricManagerId,
            FabricPlaneConnectionId), Manager.System);

        var previous = counter.BytesWritten;
        await foreach (var item in SendQueue.Reader.ReadAllAsync(Cts.Token))
        {
            item.write(writer);
            writer.Flush();
            if (Cts.IsCancellationRequested) break;

            var size = counter.BytesWritten - previous;
            previous = counter.BytesWritten;
            item.actor.EnqueueSend(size);
            SendBytesLogger.Enqueue(new(Stopwatch.Elapsed.TotalSeconds, size));
        }
        Dispose();
    }
    private async Task Enqueue(Action<BinaryWriter> write, IActor actor)
    {
        await SendQueue.Writer.WriteAsync(new(write, actor));
    }

    private async Task ReceiveLoop()
    {
        Logger.LogInformation("FabricHost {FabricPlaneConnectionId} started", FabricPlaneConnectionId);

        try
        {
            using var counter = new CountingDuplexStream(Stream);
            using var reader = new BinaryReader(counter);
            var previousBytesRead = counter.BytesRead;
            while (!Cts.IsCancellationRequested)
            {
                var messageType = PlaneFabricConverter.ReadClientToHostMessageType(reader);
                //if (Logger.IsEnabled(LogLevel.Trace))
                //    Logger.LogTrace("{now}: ReceiveLoop({messageType})", DateTime.Now.ToString("HH:mm:ss.fff"), messageType);
                switch (messageType)
                {
                    case PlaneFabricClientToHostMessageEnum.PlaneSubscribe:
                        {
                            var busSubscribe = reader.ReadPlaneSubscribeDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_PlaneSubscribe_FromApiAsync(this, busSubscribe, receiveSize, Cts.Token);
                        }
                        break;
                    case PlaneFabricClientToHostMessageEnum.PlaneUnsubscribe:
                        {
                            var busUnsubscribe = reader.ReadPlaneUnsubscribeDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_PlaneUnsubscribe_FromApiAsync(this, busUnsubscribe, receiveSize, Cts.Token);
                        }
                        break;
                    case PlaneFabricClientToHostMessageEnum.PlaneFireAndForget:
                        {
                            var busFireAndForget = reader.ReadPlaneSendRequestDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_PlaneSendFireAndForget_FromApiAsync(this, busFireAndForget, receiveSize, Cts.Token);
                        }
                        break;
                    case PlaneFabricClientToHostMessageEnum.PlaneSendRequest:
                        {
                            var busSendRequest = reader.ReadPlaneSendRequestDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_PlaneSendRequest_FromApiAsync(this, busSendRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case PlaneFabricClientToHostMessageEnum.PlaneSendRequestCancelled:
                        {
                            var busSendRequestCancelled = reader.ReadPlaneSendRequestCancelledDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_PlaneSendRequestCancelled_FromApiAsync(this, busSendRequestCancelled, receiveSize, Cts.Token);
                        }
                        break;
                    case PlaneFabricClientToHostMessageEnum.PlaneSendRequestDone:
                        {
                            var busSendRequestAck = reader.ReadPlaneSendRequestDoneDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_PlaneSendRequestDone_FromApiAsync(this, busSendRequestAck, receiveSize, Cts.Token);
                        }
                        break;
                    case PlaneFabricClientToHostMessageEnum.PlaneInvokeRequest:
                        {
                            var busInvokeRequest = reader.ReadPlaneInvokeRequestDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_PlaneInvokeRequest_FromApiAsync(this, busInvokeRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case PlaneFabricClientToHostMessageEnum.PlaneInvokeRequestCancelled:
                        {
                            var busInvokeRequest = reader.ReadPlaneInvokeRequestCancelledDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_PlaneInvokeRequestCancelled_FromApiAsync(this, busInvokeRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case PlaneFabricClientToHostMessageEnum.PlaneInvokeRequestAck:
                        {
                            var busInvokeRequestAck = reader.ReadPlaneInvokeRequestAckDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_PlaneInvokeRequestAck_FromApiAsync(this, busInvokeRequestAck, receiveSize, Cts.Token);
                        }
                        break;
                    case PlaneFabricClientToHostMessageEnum.PlaneInvokeRequestDone:
                        {
                            var busInvokeRequestDone = reader.ReadPlaneInvokeRequestDoneDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_PlaneInvokeRequestDone_FromApiAsync(this, busInvokeRequestDone, receiveSize, Cts.Token);
                        }
                        break;
                    case PlaneFabricClientToHostMessageEnum.PlaneStreamingRequest:
                        {
                            var busStreamingRequest = reader.ReadPlaneStreamingRequestDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_PlaneStreamingRequest_FromApiAsync(this, busStreamingRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case PlaneFabricClientToHostMessageEnum.PlaneStreamingResponse:
                        {
                            var busStreamingResponse = reader.ReadPlaneStreamingResponseDto(Manager.Config.MaxPackageSize ?? 512 * 1024);
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_PlaneStreamingResponse_FromApiAsync(this, busStreamingResponse, receiveSize, Cts.Token);
                        }
                        break;
                }

                var size2 = counter.BytesRead - previousBytesRead;
                previousBytesRead = counter.BytesRead;
                ReceivedBytesLogger.Enqueue(new(Stopwatch.Elapsed.TotalSeconds, size2));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "FabricClient #{FabricPlaneConnectionId.Value}: receive loop failed.", FabricPlaneConnectionId.Value);
        }
        Dispose();

        Logger.LogInformation("FabricHost {FabricPlaneConnectionId} stopped", FabricPlaneConnectionId);
    }

    public void Dispose()
    {
        Cts.Dispose();
        Stream.Dispose();
        TcpClient.Dispose();
        Manager.Disconnected(this);
    }

}
