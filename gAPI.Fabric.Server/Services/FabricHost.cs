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
using gAPI.Fabric.Server.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Channels;

namespace gAPI.Fabric.Server.Services;

public sealed class FabricHost : IFabricLoggerFactory, IActor
{
    private readonly FabricManager Manager;
    private readonly TcpClient TcpClient;
    private readonly NetworkStream Stream;
    private readonly Channel<SendQueueItem> SendQueue;
    private readonly CancellationTokenSource Cts;

    public FabricConnectionId FabricConnectionId { get; }
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
        return new (count, bytes);
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

    private FabricHostCollection Connections => Manager.Connections;
    private ConsoleBuffer Console => Manager.Console;

    public LogLevel MinimumLevel => Manager.Config.LogLevel;

    public FabricHost(
        FabricManager manager,
        TcpClient tcpClient)
    {
        Manager = manager;
        TcpClient = tcpClient;
        Cts = new CancellationTokenSource();
        Stream = tcpClient.GetStream();
        SendQueue = Channel.CreateUnbounded<SendQueueItem>();
        FabricConnectionId = Connections.AddConnection(this);
        Logger = ((ILoggerFactory)this).CreateLogger<FabricHost>();
        ManagerLogger = ((ILoggerFactory)this).CreateLogger<FabricManager>();
    }

    public void Start()
    {
        _ = Task.Run(ReceiveLoop);
        _ = Task.Run(SendLoop);
    }

    public async Task Send_SendRequest_ToApiAsync(SendRequestDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_SendRequest_ToApiAsync({request}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.FabricSendRequest);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_SendRequestCancelled_ToApiAsync(SendRequestCancelledDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_SendRequestCancelled_ToApiAsync({cancel}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.FabricSendRequestCancelled);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_SendRequestDone_ToApiAsync(SendRequestDoneDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_SendRequestDone_ToApiAsync({done}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.SendRequestDone);
            writer.Write(message);
        }, actor);
    }

    public async Task Send_InvokeRequest_ToApiAsync(InvokeRequestDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_InvokeRequest_ToApiAsync({request}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.FabricInvokeRequest);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_InvokeRequestCancelled_ToApiAsync(InvokeRequestCancelledDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_InvokeRequestCancelled_ToApiAsync({cancel}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.FabricInvokeRequestCancelled);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_InvokeRequestDone_ToApiAsync(InvokeRequestDoneDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_InvokeRequestDone_ToApiAsync({done}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.InvokeRequestDone);
            writer.Write(message);
        }, actor);
    }

    public async Task Send_StreamingRequestServerToClient_ToApiAsync(StreamingRequestDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_StreamingRequestServerToClient_ToApiAsync({request}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.StreamingRequestServerToClient);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_StreamingResponseServerToClient_ToApiAsync(StreamingResponseDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_StreamingResponseServerToClient_ToApiAsync({response}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.StreamingResponseServerToClient);
            writer.Write(message);
        }, actor);
    }

    public async Task Send_StreamingRequestClientToServer_ToApiAsync(StreamingRequestDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_StreamingRequestClientToServer_ToApiAsync({request}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.StreamingRequestClientToServer);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_StreamingResponseClientToServer_ToApiAsync(StreamingResponseDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_StreamingResponseClientToServer_ToApiAsync({response}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.StreamingResponseClientToServer);
            writer.Write(message);
        }, actor);
    }

    public async Task Send_GetSessionCookieDataResponse_ToApiAsync(SendGetSessionCookieDataResponseDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_GetSessionCookieDataResponse_ToApiAsync({response}, {actor})", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.GetSessionCookieDataResponse);
            writer.Write(message);
        }, actor);
    }
    private async Task Send_SynchronizeFabricIds_ToApiAsync(SynchronizeFabricIdsDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_SynchronizeFabricIds_ToApiAsync({ids}, {actor})", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.SynchronizeFabricIds);
            writer.Write(message);
        }, actor);
    }


    public async Task Send_ServerSendRequest_ToApiAsync(ServerSendRequestDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_ServerSendRequest_ToApiAsync({request}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.ServerSendRequest);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_ServerSendRequestDone_ToApiAsync(ServerSendRequestDoneDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_ServerSendRequestDone_ToApiAsync({done}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.ServerSendRequestDone);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_ServerInvokeRequest_ToApiAsync(ServerInvokeRequestDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_ServerInvokeRequest_ToApiAsync({request}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.ServerInvokeRequest);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_ServerInvokeRequestDone_ToApiAsync(ServerInvokeRequestDoneDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_ServerInvokeRequestDone_ToApiAsync({done}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.ServerInvokeRequestDone);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_ServerStreamingRequestClientToServer_ToApiAsync(ServerStreamingRequestDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_ServerStreamingRequestClientToServer_ToApiAsync({request}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), message, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.ServerStreamingRequest);
            writer.Write(message);
        }, actor);
    }


    private async Task SendLoop()
    {
        using var counter = new CountingDuplexStream(Stream);
        using var writer = new BinaryWriter(counter);

        await Send_SynchronizeFabricIds_ToApiAsync(new SynchronizeFabricIdsDto(
            Manager.FabricManagerId,
            FabricConnectionId), Manager.System);

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
        Console.WriteInfo($"FabricHost {FabricConnectionId} started");

        try
        {
            using var counter = new CountingDuplexStream(Stream);
            using var reader = new BinaryReader(counter);
            var previousBytesRead = counter.BytesRead;
            while (!Cts.IsCancellationRequested)
            {
                var messageType = FabricConverter.ReadClientToHostMessageType(reader);
                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.LogTrace("{now}: ReceiveLoop({messageType})", DateTime.Now.ToString("HH:mm:ss.fff"), messageType);
                switch (messageType)
                {
                    case FabricClientToHostMessageEnum.Subscribe:
                        {
                            var subscribe = reader.ReadSubscribeDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_Subscribe_FromApiAsync(this, ManagerLogger, subscribe, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.Unsubscribe:
                        {
                            var unsubscribe = reader.ReadUnsubscribeDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_Unsubscribe_FromApiAsync(this, ManagerLogger, unsubscribe, receiveSize, Cts.Token);
                        }
                        break;

                    case FabricClientToHostMessageEnum.SendRequest:
                        {
                            var sendRequest = reader.ReadSendRequestDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_SendRequest_FromApiAsync(this, ManagerLogger, sendRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.SendRequestCancelled:
                        {
                            var sendRequestCancelled = reader.ReadSendRequestCancelledDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_SendRequestCancelled_FromApiAsync(this, ManagerLogger, sendRequestCancelled, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.SendRequestDone:
                        {
                            var done = reader.ReadSendRequestDoneDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_SendRequestDone_FromApiAsync(this, ManagerLogger, done, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.InvokeRequest:
                        {
                            var invokeRequest = reader.ReadInvokeRequestDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_InvokeRequest_FromApiAsync(this, ManagerLogger, invokeRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.InvokeRequestCancelled:
                        {
                            var invokeRequestCancelled = reader.ReadInvokeRequestCancelledDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_InvokeRequestCancelled_FromApiAsync(this, ManagerLogger, invokeRequestCancelled, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.InvokeRequestDone:
                        {
                            var invokeResponseDone = reader.ReadInvokeRequestDoneDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_InvokeRequestDoneAsync(this, ManagerLogger, invokeResponseDone, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.StreamingRequestServerToClient:
                        {
                            var streamingRequest = reader.ReadStreamingRequestDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_StreamingRequestServerToClient_FromApiAsync(this, ManagerLogger, streamingRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.StreamingResponseServerToClient:
                        {
                            var streamingRequest = reader.ReadStreamingResponseDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_StreamingResponseServerToClient_FromApiAsync(this, ManagerLogger, streamingRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.StreamingRequestClientToServer:
                        {
                            var streamingRequest = reader.ReadStreamingRequestDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_StreamingRequestClientToServer_FromApiAsync(this, ManagerLogger, streamingRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.StreamingResponseClientToServer:
                        {
                            var streamingResponse = reader.ReadStreamingResponseDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_StreamingResponseClientToServer_FromApiAsync(this, ManagerLogger, streamingResponse, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.UpdateSession:
                        {
                            var updateSession = reader.ReadUpdateSessionDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_UpdateSession_FromApiAsync(this, ManagerLogger, updateSession, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.ClearSession:
                        {
                            var clearSession = reader.ReadSendClearSessionDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_ClearSession_FromApiAsync(this, ManagerLogger, clearSession, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.GetSessionCookieData:
                        {
                            var getSessionCookieData = reader.ReadSendGetSessionCookieDataDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_GetSessionCookieData_FromApiAsync(this, ManagerLogger, getSessionCookieData, receiveSize, Cts.Token);
                        }
                        break;



                    case FabricClientToHostMessageEnum.ServerSendRequest:
                        {
                            var sendRequest = reader.ReadServerSendRequestDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_ServerSendRequest_FromApiAsync(this, ManagerLogger, sendRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.ServerInvokeRequest:
                        {
                            var invokeRequest = reader.ReadServerInvokeRequestDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_ServerInvokeRequest_FromApiAsync(this, ManagerLogger, invokeRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.ServerStreamingRequest:
                        {
                            var streamingRequest = reader.ReadServerStreamingRequestDto();
                            var receiveSize = counter.BytesRead - previousBytesRead;
                            await Manager.Receive_ServerStreamingRequest_FromApiAsync(this, ManagerLogger, streamingRequest, receiveSize, Cts.Token);
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
            Console.WriteError($"FabricClient #{FabricConnectionId.Value}: receive loop failed.", ex);
        }
        Dispose();

        Console.WriteInfo($"FabricHost {FabricConnectionId} stopped");
    }

    public void Dispose()
    {
        Cts.Dispose();
        Connections.RemoveConnection(FabricConnectionId);

        Stream.Dispose();
        TcpClient.Dispose();
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
