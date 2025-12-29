using System.Net.Sockets;

namespace gAPI.Fabric;

/// <summary>
/// ClientA => AutoHubA => FabricA: 
/// void Subscribe(Guid ScopeId, Guid? UserId, string ServiceName) 
/// 
/// ClientB => AutoApiB => FabricA: 
/// void Publish(Guid? userId, string ServiceName, byte[] messageData)
/// 
/// Voor in de toekomst:
/// ClientA => AutoHubA => FabricA (ScopeId, UserId, ServiceName): "Ik wacht op MessageA" 
/// FabricA => FabricB: "Ik heb ClientA" 
/// ClientB => AutoApiB => FabricB: "Message aan ClientA" 
/// FabricB => FabricA => AutoHubA => ClientA: "Message"
/// </summary>
/// <returns></returns>
public sealed class BusClient : IAsyncDisposable
{
    private readonly BusServer BusServer;
    private readonly TcpClient TcpClient;
    private readonly NetworkStream Stream;
    private readonly BinaryReader Reader;
    private readonly BinaryWriter Writer;
    private readonly BusQueue SendQueue;

    public bool KillSwitch { get; private set; }
    public ConnectionId ClientId { get; private set; }

    public BusClient(BusServer busServer, TcpClient tcpClient)
    {
        BusServer = busServer;
        TcpClient = tcpClient;
        Stream = tcpClient.GetStream();
        Reader = new BinaryReader(Stream);
        Writer = new BinaryWriter(Stream);
        SendQueue = new BusQueue();
    }

    private void ReceiveSubscribe(ServiceId serviceId, UserId userId, ScopeId scopeId)
    {
        var subscriberId = new SubscriberId(serviceId, userId, scopeId, ClientId);
        var service = BusServer.Services.GetOrCreate(subscriberId.ServiceId);
        service.Subscribe(subscriberId, this);
    }

    private void ReceiveUnSubscribe(ServiceId serviceId, UserId userId, ScopeId scopeId)
    {
        var subscriberId = new SubscriberId(serviceId, userId, scopeId, ClientId);
        var service = BusServer.Services.TryGet(subscriberId.ServiceId);
        if (service == null)
            return;
        service.UnSubscribe(subscriberId, this);
        if (service.Users.Count == 0 && service.Scopes.Count == 0)
            BusServer.Services.Remove(subscriberId.ServiceId);
    }

    private void ReceivePublishToAll(ServiceId serviceId, byte[] messageData)
    {
        var service = BusServer.Services.TryGet(serviceId);
        if (service == null) return;
        service.Publish(serviceId, messageData);
    }
    private void ReceivePublishToUser(ServiceId serviceId, UserId userId, byte[] messageData)
    {
        var service = BusServer.Services.TryGet(serviceId);
        if (service == null) return;
        var user = service.Users.TryGet(userId);
        if (user == null) return;
        user.Publish(serviceId, messageData);
    }
    private void ReceivePublishToScope(ServiceId serviceId, ScopeId scopeId, byte[] messageData)
    {
        var service = BusServer.Services.TryGet(serviceId);
        if (service == null) return;
        var scope = service.Scopes.TryGet(scopeId);
        if (scope == null) return;
        scope.Publish(serviceId, messageData);
    }

    public void SendMessage(ServiceId serviceId, byte[] messageData)
    {
        SendQueue.Enqueue(serviceId, messageData);
    }

    #region Run Loops

    public async Task RunAsync()
    {
        ClientId = BusServer.Connections.Add(this);
        await Task.WhenAll(
            ReceiveLoop(),
            SendLoop());
        BusServer.Connections.Remove(ClientId);
    }
    private Task ReceiveLoop()
    {
        while (!KillSwitch)
        {
            switch (ReadMessageType())
            {
                case ServerMessageTypeEnum.Subscribe:
                    ReceiveSubscribe(ReadServiceId(), ReadUserId(), ReadScopeId());
                    break;
                case ServerMessageTypeEnum.UnSubscribe:
                    ReceiveUnSubscribe(ReadServiceId(), ReadUserId(), ReadScopeId());
                    break;
                case ServerMessageTypeEnum.PublishToAll:
                    ReceivePublishToAll(ReadServiceId(), ReadMessageData());
                    break;
                case ServerMessageTypeEnum.PublishToUser:
                    ReceivePublishToUser(ReadServiceId(), ReadUserId(), ReadMessageData());
                    break;
                case ServerMessageTypeEnum.PublishToScope:
                    ReceivePublishToScope(ReadServiceId(), ReadScopeId(), ReadMessageData());
                    break;
            }
        }
        return Task.CompletedTask;
    }
    private Task SendLoop()
    {
        while (!KillSwitch)
        {
            var message = SendQueue.WaitForDequeue();
            if (message == null) continue;

            Writer.Write(message.ServiceId.Value);
            Writer.Write(message.MessageData.Length);
            Writer.Write(message.MessageData);
        }
        return Task.CompletedTask;
    }

    #endregion

    #region Property Readers

    private ServerMessageTypeEnum ReadMessageType()
    {
        return (ServerMessageTypeEnum)Reader.ReadByte();
    }

    private ServiceId ReadServiceId()
    {
        var serviceName = Reader.ReadString();
        var serviceId = new ServiceId(serviceName);
        return serviceId;
    }

    private UserId ReadUserId()
    {
        var userIdNull = Reader.ReadBoolean();
        var userIdValue = userIdNull ? (Guid?)null : new Guid(Reader.ReadBytes(16));
        var userId = new UserId(userIdValue);
        return userId;
    }

    private ScopeId ReadScopeId()
    {
        var scopeIdValue = new Guid(Reader.ReadBytes(16));
        var scopeId = new ScopeId(scopeIdValue);
        return scopeId;
    }

    private byte[] ReadMessageData()
    {
        var messageLength = Reader.ReadInt32();
        var messageData = Reader.ReadBytes(messageLength);
        var messageEnd = Reader.ReadString();
        var messageValid = messageEnd == "[EndOfData]";
        if (!messageValid) throw new Exception("This looks like a buffer overflow hack. Please go away.");
        return messageData;
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        await Stream.DisposeAsync();
        TcpClient.Dispose();
    }
}

