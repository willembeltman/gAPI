using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
using gAPI.Core.Server.Dashboard;
using gAPI.Core.Server.Enums;
using gAPI.Core.Server.Fabric;
using gAPI.Core.Wss;
using gAPI.Fabric.Server.Collections;
using gAPI.Fabric.Server.Helpers;
using gAPI.Fabric.Server.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Channels;

namespace gAPI.Fabric.Server.Services;

public sealed class FabricBusHost : IFabricLoggerFactory, IActor
{
    private readonly FabricManager Manager;
    private readonly TcpClient TcpClient;
    private readonly NetworkStream Stream;
    private readonly Channel<SendQueueItem> SendQueue;
    private readonly CancellationTokenSource Cts;

    public FabricBusConnectionId FabricBusConnectionId { get; }
    public ILogger<FabricHost> Logger { get; }
    public ILogger<FabricManager> ManagerLogger { get; }
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

    private FabricBusHostCollection BusConnections => Manager.AllBusConnections;
    private ConsoleBuffer Console => Manager.Console;

    public LogLevel MinimumLevel => Manager.Config.LogLevel;

    public FabricBusHost(
        FabricManager manager,
        TcpClient tcpClient)
    {
        Manager = manager;
        TcpClient = tcpClient;
        Cts = new CancellationTokenSource();
        Stream = tcpClient.GetStream();
        SendQueue = Channel.CreateUnbounded<SendQueueItem>();
        FabricBusConnectionId = BusConnections.AddConnection(this);
        Logger = ((ILoggerFactory)this).CreateLogger<FabricHost>();
        ManagerLogger = ((ILoggerFactory)this).CreateLogger<FabricManager>();
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

    public async Task Send_BusFireAndForget_ToApiAsync(BusSendRequestDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            BusFabricConverter.WriteHostToClientMessageType(writer, BusFabricHostToClientMessageEnum.BusFireAndForget);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_BusFireAndForgetCancelled_ToApiAsync(BusSendRequestCancelledDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            BusFabricConverter.WriteHostToClientMessageType(writer, BusFabricHostToClientMessageEnum.BusFireAndForgetCancelled);
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
        Console.WriteInfo($"FabricHost {FabricBusConnectionId} started");

        try
        {
            using var counter = new CountingDuplexStream(Stream);
            using var reader = new BinaryReader(counter);
            var previousBytesRead = counter.BytesRead;
            while (!Cts.IsCancellationRequested)
            {
                var messageType = BusFabricConverter.ReadClientToHostMessageType(reader);
                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.LogTrace("{now}: ReceiveLoop({messageType})", DateTime.Now.ToString("HH:mm:ss.fff"), messageType);
                switch (messageType)
                {
                    case BusFabricClientToHostMessageEnum.BusSubscribe:
                        {
                            var busSubscribe = reader.ReadBusSubscribeDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusSubscribe_FromApiAsync(this, ManagerLogger, busSubscribe, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusUnsubscribe:
                        {
                            var busUnsubscribe = reader.ReadBusUnsubscribeDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusUnsubscribe_FromApiAsync(this, ManagerLogger, busUnsubscribe, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusFireAndForget:
                        {
                            var busFireAndForget = reader.ReadBusSendRequestDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusSendFireAndForget_FromApiAsync(this, ManagerLogger, busFireAndForget, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusFireAndForgetCancelled:
                        {
                            var busFireAndForgetCancelled = reader.ReadBusSendRequestCancelledDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusSendFireAndForgetCancelled_FromApiAsync(this, ManagerLogger, busFireAndForgetCancelled, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusSendRequest:
                        {
                            var busSendRequest = reader.ReadBusSendRequestDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusSendRequest_FromApiAsync(this, ManagerLogger, busSendRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusSendRequestCancelled:
                        {
                            var busSendRequestCancelled = reader.ReadBusSendRequestCancelledDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusSendRequestCancelled_FromApiAsync(this, ManagerLogger, busSendRequestCancelled, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusSendRequestDone:
                        {
                            var busSendRequestAck = reader.ReadBusSendRequestDoneDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusSendRequestDone_FromApiAsync(this, ManagerLogger, busSendRequestAck, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusInvokeRequest:
                        {
                            var busInvokeRequest = reader.ReadBusInvokeRequestDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusInvokeRequest_FromApiAsync(this, ManagerLogger, busInvokeRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusInvokeRequestCancelled:
                        {
                            var busInvokeRequest = reader.ReadBusInvokeRequestCancelledDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusInvokeRequestCancelled_FromApiAsync(this, ManagerLogger, busInvokeRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusInvokeRequestAck:
                        {
                            var busInvokeRequestAck = reader.ReadBusInvokeRequestAckDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusInvokeRequestAck_FromApiAsync(this, ManagerLogger, busInvokeRequestAck, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusInvokeRequestDone:
                        {
                            var busInvokeRequestDone = reader.ReadBusInvokeRequestDoneDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusInvokeRequestDone_FromApiAsync(this, ManagerLogger, busInvokeRequestDone, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusStreamingRequest:
                        {
                            var busStreamingRequest = reader.ReadBusStreamingRequestDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusStreamingRequest_FromApiAsync(this, ManagerLogger, busStreamingRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case BusFabricClientToHostMessageEnum.BusStreamingResponse:
                        {
                            var busStreamingResponse = reader.ReadBusStreamingResponseDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_BusStreamingResponse_FromApiAsync(this, ManagerLogger, busStreamingResponse, receiveSize, Cts.Token);
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
            Logger.LogError(ex, "ReceiveLoop");
            Console.WriteError($"FabricClient #{FabricBusConnectionId.Value}: receive loop failed.", ex);
        }
        Dispose();

        Console.WriteInfo($"FabricHost {FabricBusConnectionId} stopped");
    }

    public void Dispose()
    {
        Cts.Dispose();
        Stream.Dispose();
        TcpClient.Dispose();
        Manager.Disconnected(this);
    }

    public async Task Send_Log_ToServerAsync(WssLoggerLogDto dto, CancellationToken ct)
    {
        // Do not add logging lol
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.Log);
            writer.Write(dto);
        }, Manager.Logging);
    }
    public ILogger CreateLogger(string categoryName)
        => new FabricLogger(categoryName, this);
    public void AddProvider(ILoggerProvider provider)
    {
        // no-op
    }

}
