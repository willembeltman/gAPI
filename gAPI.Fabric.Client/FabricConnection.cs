using gAPI.Fabric.Types;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace gAPI.Fabric.Client;

public sealed class FabricConnection : IAsyncDisposable
{
    private readonly string Host;
    private readonly int Port;
    private readonly CancellationTokenSource Cts = new();
    private readonly ConcurrentQueue<Action<BinaryWriter>> SendQueue = new();
    private readonly HashSet<SubscriptionId> ActiveSubscriptions = new();

    private TcpClient? Tcp;
    private NetworkStream? Stream;
    private BinaryReader? Reader;
    private BinaryWriter? Writer;

    public bool IsConnected => Tcp?.Connected == true;

    public ConnectionId ClientId { get; private set; }

    public FabricConnection(string host, int port)
    {
        Host = host;
        Port = port;
    }

    #region Lifecycle

    public async Task ConnectAsync()
    {
        if (IsConnected) return;

        Tcp = new TcpClient();
        await Tcp.ConnectAsync(Host, Port);
        Stream = Tcp.GetStream();
        Reader = new BinaryReader(Stream);
        Writer = new BinaryWriter(Stream);

        _ = Task.Run(ReceiveLoop);
        _ = Task.Run(SendLoop);

        // restore subscriptions
        foreach (var sub in ActiveSubscriptions)
            SendSubscribe(sub);
    }

    public async Task DisconnectAsync()
    {
        Cts.Cancel();
        Tcp?.Close();
        await DisposeAsync();
    }

    #endregion



    #region Gateway

    private void SendSubscribe(ServiceId serviceId, UserId userId, ScopeId scopeId)
    {
        var sub = new SubscriptionId(serviceId, userId, scopeId, ClientId);
        ActiveSubscriptions.Add(sub);
        SendSubscribe(sub);
    }

    private void SendSubscribe(SubscriptionId sub)
    {
        Enqueue(w =>
        {
            w.Write((byte)ReceivedMessageType.Subscribe);
            WriteServiceId(w, sub.ServiceId);
            WriteUserId(w, sub.UserId);
            WriteScopeId(w, sub.ScopeId);
        });
    }

    private void SendUnSubscribe(ServiceId serviceId, UserId userId, ScopeId scopeId)
    {
        var sub = new SubscriptionId(serviceId, userId, scopeId, ClientId);
        ActiveSubscriptions.Remove(sub);
        Enqueue(w =>
        {
            w.Write((byte)ReceivedMessageType.UnSubscribe);
            WriteServiceId(w, serviceId);
            WriteUserId(w, userId);
            WriteScopeId(w, scopeId);
        });
    }

    private void SendPublishToAll(ServiceId serviceId, byte[] data)
    {
        Enqueue(w =>
        {
            w.Write((byte)ReceivedMessageType.PublishToAll);
            WriteServiceId(w, serviceId);
            WriteMessageData(w, data);
        });
    }

    private void SendPublishToUser(ServiceId serviceId, UserId userId, byte[] data)
    {
        Enqueue(w =>
        {
            w.Write((byte)ReceivedMessageType.PublishToUser);
            WriteServiceId(w, serviceId);
            WriteUserId(w, userId);
            WriteMessageData(w, data);
        });
    }

    private void SendPublishToScope(ServiceId serviceId, ScopeId scopeId, byte[] data)
    {
        Enqueue(w =>
        {
            w.Write((byte)ReceivedMessageType.PublishToScope);
            WriteServiceId(w, serviceId);
            WriteScopeId(w, scopeId);
            WriteMessageData(w, data);
        });
    }

    #endregion

    #region Loops

    private async Task SendLoop()
    {
        while (!Cts.IsCancellationRequested)
        {
            if (!SendQueue.TryDequeue(out var write))
            {
                await Task.Delay(1);
                continue;
            }

            write(Writer!);
            Writer!.Flush();
        }
    }

    private Task ReceiveLoop()
    {
        try
        {
            ClientId = ReadClientId(Reader!);
            while (!Cts.IsCancellationRequested)
            {
                var serviceId = ReadServiceId(Reader!);
                var data = ReadMessageData(Reader!);
                OnMessage?.Invoke(serviceId, data);
            }
        }
        catch { }

        return Task.CompletedTask;
    }

    #endregion

    #region Events

    public event Action<ServiceId, byte[]>? OnMessage;

    #endregion

    #region Wire helpers


    private void Enqueue(Action<BinaryWriter> write)
    {
        SendQueue.Enqueue(write);
    }

    private static void WriteServiceId(BinaryWriter w, ServiceId id)
    {
        w.Write(id.Value);
    }
    private static void WriteUserId(BinaryWriter w, UserId id)
    {
        w.Write(id.Value == null);
        if (id.Value != null) w.Write(id.Value.Value.ToByteArray());
    }
    private static void WriteScopeId(BinaryWriter w, ScopeId id)
    {
        w.Write(id.Value.ToByteArray());
    }
    private static void WriteMessageData(BinaryWriter w, byte[] data)
    {
        w.Write(data.Length);
        w.Write(data);
        w.Write("[EndOfData]");
    }


    private ConnectionId ReadClientId(BinaryReader binaryReader)
    {
        return new ConnectionId(binaryReader.ReadInt64());
    }
    private static ServiceId ReadServiceId(BinaryReader r)
    {
        return new(r.ReadString());
    }
    private static byte[] ReadMessageData(BinaryReader r)
    {
        var len = r.ReadInt32();
        var data = r.ReadBytes(len);
        if (r.ReadString() != "[EndOfData]") throw new Exception("Invalid frame");
        return data;
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        Cts.Cancel();
        Stream?.Dispose();
        Tcp?.Dispose();
        Cts.Dispose();
        await Task.CompletedTask;
    }
}
