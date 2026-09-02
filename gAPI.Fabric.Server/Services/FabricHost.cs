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
    private readonly Channel<(Action<BinaryWriter> write, IActor? actor)> SendQueue;
    private readonly CancellationTokenSource Cts;

    public FabricConnectionId FabricConnectionId { get; }
    public ILogger<FabricHost> Logger { get; }
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
        SendQueue = Channel.CreateUnbounded<(Action<BinaryWriter> write, IActor? actor)>();
        FabricConnectionId = Connections.AddConnection(this);
        Logger = ((ILoggerFactory)this).CreateLogger<FabricHost>();
    }

    public void Start()
    {
        _ = Task.Run(ReceiveLoop);
        _ = Task.Run(SendLoop);
    }

    public async Task Send_GetSessionCookieDataResponse_ToApiAsync(SendGetSessionCookieDataResponseDto getSessionCookieDataResponse, IActor? actor)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + $" Send({Id}) SendGetSessionCookieDataResponseAsync({{getSessionCookieDataResponse}})", getSessionCookieDataResponse);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.GetSessionCookieDataResponse);
            writer.Write(getSessionCookieDataResponse);
        }, actor);
    }

    public async Task Send_SendRequest_ToApiAsync(SendRequestDto message, IActor actor)
    {
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.SendRequest);
            writer.Write(message);
        }, actor);
    }
    public async Task Send_SendRequestDone_ToApiAsync(SendRequestDoneDto done, IActor? actor)
    {
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.SendRequestDone);
            writer.Write(done);
        }, actor);
    }

    public async Task Send_StreamingRequest_ToApiAsync(StreamingRequestDto request, IActor? actor)
    {
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.StreamingRequest);
            writer.Write(request);
        }, actor);
    }
    public async Task Send_StreamingResponse_ToApiAsync(StreamingResponseDto response, IActor? actor)
    {
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.StreamingResponse);
            writer.Write(response);
        }, actor);
    }

    public async Task Send_InvokeRequest_ToApiAsync(InvokeRequestDto request, IActor actor)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + $" Send({Id}) Send_InvokeRequest_ToFabricAsync({{request}})", request);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.InvokeRequest);
            writer.Write(request);
        }, actor);
    }
    public async Task Send_InvokeResponse_ToApiAsync(InvokeResponseDto response, IActor? actor)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + $" Send({Id}) InvokeResponseAsync({{response}})", response);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.InvokeResponse);
            writer.Write(response);
        }, actor);
    }
    public async Task Send_InvokeResponseDone_ToApiAsync(InvokeResponseDoneDto done, IActor? actor)
    {
        //if (Logger.IsEnabled(LogLevel.Trace))
        //    Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + $" Send({Id}) InvokeResponseDoneAsync({{requestId}})", requestId);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.InvokeResponseDone);
            writer.Write(done);
        }, actor);
    }

    private async Task SendLoop()
    {
        using var counter = new CountingDuplexStream(Stream);
        using var writer = new BinaryWriter(counter);

        FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.SynchronizeFabricIds);
        writer.Write(new SynchronizeFabricIdsDto(
            Manager.FabricManagerId,
            FabricConnectionId));

        var previous = counter.BytesWritten;
        await foreach (var item in SendQueue.Reader.ReadAllAsync(Cts.Token))
        {
            item.write(writer);
            writer.Flush();
            if (Cts.IsCancellationRequested) break;

            var size = counter.BytesWritten - previous;
            previous = counter.BytesWritten;
            item.actor?.EnqueueSend(size);
            SendLogger.Enqueue(new(Stopwatch.Elapsed.TotalSeconds, size));
        }
        Dispose();
    }
    private async Task Enqueue(Action<BinaryWriter> write, IActor? actor = null)
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
                switch (FabricConverter.ReadClientToHostMessageType(reader))
                {
                    case FabricClientToHostMessageEnum.Subscribe:
                        {
                            var subscribe = reader.ReadSubscribeDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_SubscribeAsync(this, subscribe, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.Unsubscribe:
                        {
                            var unsubscribe = reader.ReadUnsubscribeDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_UnsubscribeAsync(this, unsubscribe, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.SendRequest:
                        {
                            var sendRequest = reader.ReadSendRequestDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_Send_SendRequest_ToServiceAsync(this, sendRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.SendRequestDone:
                        {
                            var done = reader.ReadSendRequestDoneDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_SendRequestDoneAsync(this, done, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.StreamingRequest:
                        {
                            var argumentRequest = reader.ReadStreamingRequestDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_StreamingRequestAsync(this, argumentRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.StreamingResponse:
                        {
                            var argumentResponse = reader.ReadStreamingResponseDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_StreamingResponseAsync(this, argumentResponse, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.InvokeRequest:
                        {
                            var invokeRequest = reader.ReadInvokeRequestDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_InvokeRequest_FromApiAsync(this, invokeRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.InvokeResponse:
                        {
                            var invokeResponse = reader.ReadInvokeResponseDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_InvokeResponseAsync(this, invokeResponse, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.InvokeResponseDone:
                        {
                            var invokeResponseDone = reader.ReadInvokeResponseDoneDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_InvokeResponseDoneAsync(this, invokeResponseDone, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.UpdateSession:
                        {
                            var updateSession = reader.ReadUpdateSessionDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_UpdateSessionAsync(this, updateSession, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.ClearSession:
                        {
                            var clearSession = reader.ReadSendClearSessionDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_ClearSessionAsync(this, clearSession, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.GetSessionCookieData:
                        {
                            var getSessionCookieData = reader.ReadSendGetSessionCookieDataDto();
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.Receive_GetSessionCookieDataAsync(this, getSessionCookieData, receiveSize, Cts.Token);
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

    public async Task Send_Log_ToServerAsync(WssLoggerLogDto dto, CancellationToken ct = default)
    {
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.Log);
            writer.Write(dto);
        });
    }
    public ILogger CreateLogger(string categoryName)
        => new FabricLogger(categoryName, this);
    public void AddProvider(ILoggerProvider provider)
    {
        // no-op
    }
}
