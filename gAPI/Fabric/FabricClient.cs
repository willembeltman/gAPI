using gAPI.Ids;
using gAPI.Sse;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Channels;

namespace gAPI.Fabric;

public sealed class FabricClient : IAsyncDisposable
{
    private readonly FabricConverter Fc = new();
    private readonly Channel<Action<BinaryWriter>> SendQueue = Channel.CreateUnbounded<Action<BinaryWriter>>();
    private readonly ConcurrentDictionary<SseServiceId, ConcurrentDictionary<SseHostId, SseHost>> Services = new();
    private readonly string? Host;
    private readonly int? Port;
    private readonly CancellationTokenSource SendCts = new();
    private CancellationTokenSource? ReceiveCts;
    private TcpClient? Tcp;
    private NetworkStream? Stream;
    private BinaryReader? Reader;
    private BinaryWriter? Writer;
    private bool FirstTime;
    private bool IsConnecting;
    private bool IsDisconnecting;

    public FabricClient()
    {
    }
    public FabricClient(string host, int port)
    {
        Host = host;
        Port = port;
    }

    public FabricHostId Id { get; set; }
    public bool IsConnected => IsDisconnecting || IsConnecting || Tcp?.Connected == true;

    public void ConnectAsync()
    {
        if (Host == null || Port == null) return;
        if (IsConnected || IsConnecting || IsDisconnecting) return;

        try
        {
            Console.WriteLine($"Starting FabricClient");

            IsConnecting = true;

            ReceiveCts = new CancellationTokenSource();
            Tcp = new TcpClient();
            Tcp.Connect(Host, Port.Value);
            Stream = Tcp.GetStream();
            Reader = new BinaryReader(Stream);
            Writer = new BinaryWriter(Stream);

            if (!FirstTime)
            {
                FirstTime = true;
                _ = Task.Run(SendKernel);
            }

            _ = Task.Run(ReceiveKernel);
        }
        finally
        {
            IsConnecting = false;
        }
    }
    private void Reconnect()
    {
        Console.WriteLine($"Reconnecting FabricClient ....");
        Disconnect();
        ConnectAsync();
        foreach (var service in Services.Values)
        {
            foreach (var sseHost in service.Values)
            {
                Console.WriteLine(
                    $"Resubscribe " +
                    $"SseHost {sseHost.Id} to " +
                    $"{sseHost.ServiceId} " +
                    $"(userId {sseHost.UserId}, " +
                    $"sessionId {sseHost.SessionId})");
                SubscribeToFabric(sseHost);
            }
        }
        Console.WriteLine($"Reconnecting FabricClient DONE");
    }
    public void Disconnect()
    {
        if (IsConnecting) return;

        try
        {
            Console.WriteLine($"Disconnecting FabricClient {Id}");

            IsDisconnecting = true;

            ReceiveCts?.Cancel();
            ReceiveCts?.Dispose();
            ReceiveCts = null;

            Reader?.Dispose();
            Reader = null;

            Writer?.Dispose();
            Writer = null;

            Stream?.Dispose();
            Stream = null;

            Tcp?.Dispose();
            Tcp = null;
        }
        finally
        {
            IsDisconnecting = false;
        }
    }

    #region Kernels
    private async Task ReceiveKernel()
    {
        if (Reader == null) return;
        if (ReceiveCts == null) return;
        if (Stream == null) return;
        try
        {
            Id = Fc.ReadFabricHostId(Reader!);

            Console.WriteLine($"FabricClient {Id.Value} started");

            while (!ReceiveCts.IsCancellationRequested)
            {
                var t = Fc.ReadHostToClientMessageType(Reader);
                switch (t)
                {
                    case FabricHostToClientMessageEnum.SendSseMessageToClient:
                        var message = new SseMessage(
                            Fc.ReadServiceId(Reader),
                            Fc.ReadServiceMethodId(Reader),
                            Fc.ReadNullableUserId(Reader),
                            Fc.ReadNullableSessionId(Reader),
                            Fc.ReadMessageData(Reader));
                        SendSseMessageToClient(message);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"FabricClient #{Id.Value}: Exception occured, restarting fabric client");
            Console.WriteLine($"{ex}");
            Console.WriteLine();
        }

        Reconnect();
    }
    private async Task SendKernel()
    {
        await foreach (var item in SendQueue.Reader.ReadAllAsync(SendCts.Token))
        {
            while (Writer == null)
            {
                if (SendCts.IsCancellationRequested) return;
                await Task.Delay(10);
            }
            item(Writer);
            Writer.Flush();
        }
    }
    #endregion

    public void Subscribe(SseHost sseHost)
    {
        var sseHostsForService = Services.AddOrUpdate(
            sseHost.ServiceId,
            new ConcurrentDictionary<SseHostId, SseHost>(),
            (a, b) => b);
        sseHostsForService[sseHost.Id] = sseHost;
        Console.WriteLine(
            $"Subscribe " +
            $"SseHost {sseHost.Id} to " +
            $"{sseHost.ServiceId} " +
            $"(userId {sseHost.UserId}, " +
            $"sessionId {sseHost.SessionId})");

        if (Host == null)
        {
            return;
        }

        SubscribeToFabric(sseHost);
    }
    public void Unsubscribe(SseHost sseHost)
    {
        Services[sseHost.ServiceId].TryRemove(sseHost.Id, out _);
        Console.WriteLine(
            $"Unsubscribe " +
            $"SseHost {sseHost.Id} from " +
            $"{sseHost.ServiceId} " +
            $"(userId {sseHost.UserId}, " +
            $"sessionId {sseHost.SessionId})");

        if (Host == null)
        {
            return;
        }

        UnsubscribeFromFabric(sseHost);
    }
    public void Publish(SseServiceId serviceId, SseServiceMethodId serviceMethodId, UserId? userId, SessionId? sessionId, string data)
    {
        Console.WriteLine(
            $"Publish " +
            $"FabricClient {Id} to " +
            $"{serviceId} " +
            $"(userId {userId}, " +
            $"sessionId {sessionId})");

        if (Host == null)
        {
            SendSseMessageToClient(new SseMessage(serviceId, serviceMethodId, userId, sessionId, data));
            return;
        }

        PublishToFabric(serviceId, serviceMethodId, userId, sessionId, data);
    }

    #region Client => Host
    private void SubscribeToFabric(SseHost sseHost)
    {
        Enqueue(w =>
        {
            Fc.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.Subscribe);
            Fc.WriteServiceId(w, sseHost.ServiceId);
            Fc.WriteUserId(w, sseHost.UserId);
            Fc.WriteSessionId(w, sseHost.SessionId);
        });
    }
    private void UnsubscribeFromFabric(SseHost sseHost)
    {
        Enqueue(w =>
        {
            Fc.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.UnSubscribe);
            Fc.WriteServiceId(w, sseHost.ServiceId);
            Fc.WriteUserId(w, sseHost.UserId);
            Fc.WriteSessionId(w, sseHost.SessionId);
        });
    }
    private void PublishToFabric(SseServiceId serviceId, SseServiceMethodId serviceMethodId, UserId? userId, SessionId? sessionId, string data)
    {
        Enqueue(w =>
        {
            //Console.WriteLine($"{DateTime.Now:HH:mm:ss.FFF}: FabricClient.Publish (execute)");
            Fc.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.Publish);
            Fc.WriteServiceId(w, serviceId);
            Fc.WriteServiceMethodId(w, serviceMethodId);
            Fc.WriteNullableUserId(w, userId);
            Fc.WriteNullableSessionId(w, sessionId);
            Fc.WriteMessageData(w, data);
        });
    }
    private void Enqueue(Action<BinaryWriter> write)
    {
        SendQueue.Writer.TryWrite(write);
    }
    #endregion

    #region Host => Client
    public void SendSseMessageToClient(SseMessage message)
    {
        foreach (var sseHost in Services[message.ServiceId].Values)
        {
            sseHost.Channel.Writer.TryWrite(new SseEvent("SseMessage", message));
        }
    }
    #endregion

    public async ValueTask DisposeAsync()
    {
        Console.WriteLine($"Closing FabricClient {Id}");
        Disconnect();
        await SendCts.CancelAsync();
        SendCts.Dispose();
    }
}