using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Interfaces;
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

public sealed class FabricHost : IFabricLoggerFactory
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

    private readonly ConcurrentQueue<(double time, long bytes)> SendLogger = new();
    private readonly ConcurrentQueue<(double time, long bytes)> ReceiveLogger = new();

    private string GetSpeed(ConcurrentQueue<(double time, long bytes)> queue)
    {
        var interval = 1.0;
        var now = Stopwatch.Elapsed.TotalSeconds;

        // Verwijder oude entries
        while (queue.TryPeek(out var entry) && entry.time < now - interval)
            queue.TryDequeue(out _);

        var bytes = queue.Sum(x => x.bytes);

        return bytes switch
        {
            < 1024 => $"{bytes}b/sec",
            < 1024 * 1024 => $"{bytes / 1024}kb/sec",
            < 1024L * 1024 * 1024 => $"{bytes / (1024 * 1024)}mb/sec",
            < 1024L * 1024 * 1024 * 1024 => $"{bytes / (1024L * 1024 * 1024)}gb/sec",
            _ => $"{bytes / (1024L * 1024 * 1024 * 1024)}tb/sec"
        };
    }
    public string GetSendSpeed() => GetSpeed(SendLogger);
    public string GetReceiveSpeed() => GetSpeed(ReceiveLogger);

    private FabricHostCollection Connections => Manager.Connections;
    private IConsole Console => Manager.Console;

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

    public async Task Send_SendRequest_ToApiAsync(SendRequestDto request, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_SendRequest_ToApiAsync({request}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), request, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.FabricSendRequest);
            writer.Write(request);
        }, actor);
    }
    public async Task Send_SendRequestCancelled_ToApiAsync(SendRequestCancelledDto cancel, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_SendRequestCancelled_ToApiAsync({cancel}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), cancel, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.FabricSendRequestCancelled);
            writer.Write(cancel);
        }, actor);
    }
    public async Task Send_SendRequestDone_ToApiAsync(SendRequestDoneDto done, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_SendRequestDone_ToApiAsync({done}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), done, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.SendRequestDone);
            writer.Write(done);
        }, actor);
    }

    public async Task Send_InvokeRequest_ToApiAsync(InvokeRequestDto request, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_InvokeRequest_ToApiAsync({request}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), request, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.FabricInvokeRequest);
            writer.Write(request);
        }, actor);
    }
    public async Task Send_InvokeRequestCancelled_ToApiAsync(InvokeRequestCancelledDto cancel, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_InvokeRequestCancelled_ToApiAsync({cancel}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), cancel, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.FabricInvokeRequestCancelled);
            writer.Write(cancel);
        }, actor);
    }
    public async Task Send_InvokeRequestDone_ToApiAsync(InvokeRequestDoneDto done, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_InvokeRequestDone_ToApiAsync({done}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), done, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.InvokeRequestDone);
            writer.Write(done);
        }, actor);
    }

    public async Task Send_StreamingRequestServerToClient_ToApiAsync(StreamingRequestDto request, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_StreamingRequestServerToClient_ToApiAsync({request}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), request, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.StreamingRequestServerToClient);
            writer.Write(request);
        }, actor);
    }
    public async Task Send_StreamingResponseServerToClient_ToApiAsync(StreamingResponseDto response, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_StreamingResponseServerToClient_ToApiAsync({response}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), response, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.StreamingResponseServerToClient);
            writer.Write(response);
        }, actor);
    }

    public async Task Send_StreamingRequestClientToServer_ToApiAsync(StreamingRequestDto request, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_StreamingRequestClientToServer_ToApiAsync({request}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), request, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.StreamingRequestClientToServer);
            writer.Write(request);
        }, actor);
    }
    public async Task Send_StreamingResponseClientToServer_ToApiAsync(StreamingResponseDto response, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_StreamingResponseClientToServer_ToApiAsync({response}, {actor}", DateTime.Now.ToString("HH:mm:ss.fff"), response, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.StreamingResponseClientToServer);
            writer.Write(response);
        }, actor);
    }

    public async Task Send_GetSessionCookieDataResponse_ToApiAsync(SendGetSessionCookieDataResponseDto response, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_GetSessionCookieDataResponse_ToApiAsync({response}, {actor})", DateTime.Now.ToString("HH:mm:ss.fff"), response, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.GetSessionCookieDataResponse);
            writer.Write(response);
        }, actor);
    }
    private async Task Send_SynchronizeFabricIds_ToApiAsync(SynchronizeFabricIdsDto ids, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{now}: Send_SynchronizeFabricIds_ToApiAsync({ids}, {actor})", DateTime.Now.ToString("HH:mm:ss.fff"), ids, actor);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.SynchronizeFabricIds);
            writer.Write(ids);
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
            SendLogger.Enqueue(new(Stopwatch.Elapsed.TotalSeconds, size));
        }
        Dispose();
    }
    private async Task Enqueue(Action<BinaryWriter> write, IActor actor)
    {
        await SendQueue.Writer.WriteAsync(new(write, actor));
    }

    private async Task ReceiveLoop()
    {
        Console.WriteLine();
        Console.WriteLine($"FabricHost {FabricConnectionId} started");
        Console.WriteLine();

        try
        {
            using var counter = new CountingDuplexStream(Stream);
            using var reader = new BinaryReader(counter);
            var previous = counter.BytesRead;
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
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_Subscribe_FromApiAsync(this, ManagerLogger, subscribe, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.Unsubscribe:
                        {
                            var unsubscribe = reader.ReadUnsubscribeDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_Unsubscribe_FromApiAsync(this, ManagerLogger, unsubscribe, receiveSize, Cts.Token);
                        }
                        break;

                    case FabricClientToHostMessageEnum.SendRequest:
                        {
                            var sendRequest = reader.ReadSendRequestDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_SendRequest_FromApiAsync(this, ManagerLogger, sendRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.SendRequestCancelled:
                        {
                            var sendRequestCancelled = reader.ReadSendRequestCancelledDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_SendRequestCancelled_FromApiAsync(this, ManagerLogger, sendRequestCancelled, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.SendRequestDone:
                        {
                            var done = reader.ReadSendRequestDoneDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_SendRequestDone_FromApiAsync(this, ManagerLogger, done, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.InvokeRequest:
                        {
                            var invokeRequest = reader.ReadInvokeRequestDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_InvokeRequest_FromApiAsync(this, ManagerLogger, invokeRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.InvokeRequestCancelled:
                        {
                            var invokeRequestCancelled = reader.ReadInvokeRequestCancelledDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_InvokeRequestCancelled_FromApiAsync(this, ManagerLogger, invokeRequestCancelled, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.InvokeRequestDone:
                        {
                            var invokeResponseDone = reader.ReadInvokeRequestDoneDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_InvokeRequestDoneAsync(this, ManagerLogger, invokeResponseDone, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.StreamingRequestServerToClient:
                        {
                            var streamingRequest = reader.ReadStreamingRequestDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_StreamingRequestServerToClient_FromApiAsync(this, ManagerLogger, streamingRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.StreamingResponseServerToClient:
                        {
                            var streamingRequest = reader.ReadStreamingResponseDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_StreamingResponseServerToClient_FromApiAsync(this, ManagerLogger, streamingRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.StreamingRequestClientToServer:
                        {
                            var streamingRequest = reader.ReadStreamingRequestDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_StreamingRequestClientToServer_FromApiAsync(this, ManagerLogger, streamingRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.StreamingResponseClientToServer:
                        {
                            var streamingResponse = reader.ReadStreamingResponseDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_StreamingResponseClientToServer_FromApiAsync(this, ManagerLogger, streamingResponse, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.UpdateSession:
                        {
                            var updateSession = reader.ReadUpdateSessionDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_UpdateSession_FromApiAsync(this, ManagerLogger, updateSession, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.ClearSession:
                        {
                            var clearSession = reader.ReadSendClearSessionDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_ClearSession_FromApiAsync(this, ManagerLogger, clearSession, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.GetSessionCookieData:
                        {
                            var getSessionCookieData = reader.ReadSendGetSessionCookieDataDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_GetSessionCookieData_FromApiAsync(this, ManagerLogger, getSessionCookieData, receiveSize, Cts.Token);
                        }
                        break;
                }

                var size2 = counter.BytesRead - previous;
                previous = counter.BytesRead;
                ReceiveLogger.Enqueue(new(Stopwatch.Elapsed.TotalSeconds, size2));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ReceiveLoop");
            Console.WriteLine();
            Console.WriteLine($"FabricClient #{FabricConnectionId.Value}: Exception occured, restarting fabric client", ConsoleColor.Red);
            Console.WriteLine($"{ex}");
            Console.WriteLine();
        }
        Dispose();

        Console.WriteLine();
        Console.WriteLine($"!FabricHost {FabricConnectionId} stopped");
        Console.WriteLine();
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
