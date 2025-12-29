using gAPI.Fabric.Collections;
using gAPI.Fabric.Types;
using System.Net.Sockets;

namespace gAPI.Fabric.Models;

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
public sealed class Connection : IAsyncDisposable
{
    private readonly State State;
    private readonly TcpClient TcpClient;
    private readonly NetworkStream Stream;
    private readonly BinaryReader Reader;
    private readonly BinaryWriter Writer;
    private readonly SendQueue SendQueue;

    public ConnectionId Id { get; }
    public bool KillSwitch { get; private set; }

    public Connection(State state, TcpClient tcpClient)
    {
        State = state;
        TcpClient = tcpClient;
        Stream = tcpClient.GetStream();
        Reader = new BinaryReader(Stream);
        Writer = new BinaryWriter(Stream);
        SendQueue = new SendQueue();
        Id = State.AddConnection(this);
    }

    public void SendMessage(ServiceId serviceId, byte[] messageData)
    {
        SendQueue.Enqueue(serviceId, messageData);
    }

    public async Task RunAsync()
    {
        await Task.WhenAll(
            ReceiveLoop(),
            SendLoop());
        State.RemoveConnection(this);
    }
    private Task ReceiveLoop()
    {
        while (!KillSwitch)
        {
            switch (ReadMessageType())
            {
                case ReceivedMessageType.Subscribe:
                    State.Subscribe(ReadServiceId(), ReadUserId(), ReadScopeId(), this);
                    break;
                case ReceivedMessageType.UnSubscribe:
                    State.UnSubscribe(ReadServiceId(), ReadUserId(), ReadScopeId(), this);
                    break;
                case ReceivedMessageType.PublishToAll:
                    State.PublishToAll(ReadServiceId(), ReadMessageData());
                    break;
                case ReceivedMessageType.PublishToUser:
                    State.PublishToUser(ReadServiceId(), ReadUserId(), ReadMessageData());
                    break;
                case ReceivedMessageType.PublishToScope:
                    State.PublishToScope(ReadServiceId(), ReadScopeId(), ReadMessageData());
                    break;
            }
        }
        return Task.CompletedTask;
    }
    private Task SendLoop()
    {
        foreach (var message in SendQueue)
        {
            Writer.Write(message.ServiceId.Value);
            Writer.Write(message.Data.Length);
            Writer.Write(message.Data);
            if (KillSwitch) break;
        }
        return Task.CompletedTask;
    }

    #region Property Readers

    private ReceivedMessageType ReadMessageType()
    {
        return (ReceivedMessageType)Reader.ReadByte();
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
        SendQueue.Dispose();
    }
}

