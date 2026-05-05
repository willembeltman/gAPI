using gAPI.Core.Interfaces;
using gAPI.Core.Dtos;
using gAPI.Core.Server.Enums;
using gAPI.Core.Server.Fabric;
using gAPI.Fabric.Server.Collections;
using gAPI.Core.Wss;
using gAPI.Core.Ids;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Channels;
using gAPI.Fabric.Server.Interfaces;
using gAPI.Fabric.Server.Helpers;

namespace gAPI.Fabric.Server.Models;

public sealed class FabricHost : IWssLoggerFactory
{
    private readonly FabricManager Manager;
    private readonly TcpClient TcpClient;
    private readonly FabricHostCollection Connections;
    private readonly IConsole Console;
    private readonly CancellationTokenSource Cts;
    private readonly NetworkStream Stream;
    private readonly Channel<(Action<BinaryWriter> write, IActor? actor)> SendQueue;


    public FabricHostId Id { get; }
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


    public FabricHost(
        FabricManager manager,
        TcpClient tcpClient,
        FabricHostCollection connections,
        IConsole console)
    {
        Manager = manager;
        TcpClient = tcpClient;
        Connections = connections;
        Console = console;
        Cts = new CancellationTokenSource();
        Stream = tcpClient.GetStream();
        SendQueue = Channel.CreateUnbounded<(Action<BinaryWriter> write, IActor? actor)>();
        Id = Connections.AddConnection(this);
        Logger = ((ILoggerFactory)this).CreateLogger<FabricHost>();
    }

    public void Start()
    {
        _ = Task.Run(ReceiveLoop);
        _ = Task.Run(SendLoop);
    }

    public async Task SendRequestAsync(SendRequestDto message, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + $" Send({Id}) SendRequestAsync({{message}})", message);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.SendRequest);
            writer.Write(message);
        }, actor);
    }
    public async Task InvokeRequestAsync(InvokeRequestDto request, IActor actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + $" Send({Id}) InvokeRequestAsync({{request}})", request);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.InvokeRequest);
            writer.Write(request);
        }, actor);
    }
    public async Task InvokeResponseAsync(InvokeResponseDto response, IActor? actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + $" Send({Id}) InvokeResponseAsync({{response}})", response);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.InvokeResponse);
            writer.Write(response);
        }, actor);
    }
    public async Task InvokeResponseDoneAsync(RequestId requestId, IActor? actor)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + $" Send({Id}) InvokeResponseDoneAsync({{requestId}})", requestId);
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.InvokeResponseDone);
            writer.Write(requestId);
        }, actor);
    }
    public async Task Send_Log_ToServerAsync(WssLoggerLogDto dto, CancellationToken ct = default)
    {
        await Enqueue(writer =>
        {
            FabricConverter.WriteHostToClientMessageType(writer, FabricHostToClientMessageEnum.Log);
            writer.Write(dto);
        });
    }

    private async Task SendLoop()
    {
        using var counter = new CountingDuplexStream(Stream);
        using var writer = new BinaryWriter(counter);
        FabricConverter.WriteFabricHostId(writer, Id);
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
        Console.WriteLine($"FabricHost {Id} started");
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
                            if (Logger.IsEnabled(LogLevel.Trace))
                                Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + $" ReceiveLoop({Id}) Subscribe({{subscribe}})", subscribe);
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.SubscribeAsync(this, subscribe, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.Unsubscribe:
                        {
                            var unsubscribe = reader.ReadUnsubscribeDto();
                            if (Logger.IsEnabled(LogLevel.Trace))
                                Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + $" ReceiveLoop({Id}) Unsubscribe({{unsubscribe}})", unsubscribe);
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.UnsubscribeAsync(this, unsubscribe, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.SendRequest:
                        {
                            var sendRequest = reader.ReadSendRequestDto();
                            if (Logger.IsEnabled(LogLevel.Trace))
                                Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + $" ReceiveLoop({Id}) SendRequest({{sendRequest}})", sendRequest);
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.SendRequestAsync(this, sendRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.InvokeRequest:
                        {
                            var invokeRequest = reader.ReadInvokeRequestDto();
                            if (Logger.IsEnabled(LogLevel.Trace))
                                Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + $" ReceiveLoop({Id}) InvokeRequest({{invokeRequest}})", invokeRequest);
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.InvokeRequestAsync(this, invokeRequest, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.InvokeResponse:
                        {
                            var invokeResponse = reader.ReadInvokeResponseDto();
                            if (Logger.IsEnabled(LogLevel.Trace))
                                Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + $" ReceiveLoop({Id}) InvokeResponse({{invokeResponse}})", invokeResponse);
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.InvokeResponseAsync(this, invokeResponse, receiveSize, Cts.Token);
                        }
                        break;
                    case FabricClientToHostMessageEnum.InvokeResponseDone:
                        {
                            var requestId = reader.ReadRequestId();
                            if (Logger.IsEnabled(LogLevel.Trace))
                                Logger.LogTrace(DateTime.Now.ToString("HH:mm:ss.fff") + $" Receive({Id}) InvokeResponseDone({{requestId}})", requestId);
                            var receiveSize = counter.BytesRead - previous;
                            await Manager.InvokeResponseDoneAsync(this, requestId, receiveSize, Cts.Token);
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
            Console.WriteLine($"FabricClient #{Id.Value}: Exception occured, restarting fabric client", ConsoleColor.Red);
            Console.WriteLine($"{ex}");
            Console.WriteLine();
        }
        Dispose();

        Console.WriteLine();
        Console.WriteLine($"!FabricHost {Id} stopped");
        Console.WriteLine();
    }

    public ILogger CreateLogger(string categoryName)
        => new WssLogger(categoryName, this);
    public void AddProvider(ILoggerProvider provider)
    {
        // no-op
    }
    public void Dispose()
    {
        Cts.Dispose();
        Connections.RemoveConnection(Id);

        Stream.Dispose();
        TcpClient.Dispose();
    }
}
