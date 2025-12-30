namespace gAPI.Fabric.Types;

public class FabricConverter
{
    public ReceivedMessageType ReadMessageType(BinaryReader Reader)
    {
        return (ReceivedMessageType)Reader.ReadByte();
    }
    public FabricConnectionId ReadFabricConnectionId(BinaryReader binaryReader)
    {
        return new FabricConnectionId(binaryReader.ReadInt64());
    }
    public ServiceId ReadServiceId(BinaryReader Reader)
    {
        var serviceName = Reader.ReadString();
        var serviceId = new ServiceId(serviceName);
        return serviceId;
    }
    public UserId ReadUserId(BinaryReader Reader)
    {
        if (Reader.ReadBoolean()) return new UserId(null);
        return new UserId(Reader.ReadString());
    }
    public UserId? ReadNullableUserId(BinaryReader Reader)
    {
        if (Reader.ReadBoolean()) return null;
        if (Reader.ReadBoolean()) return new UserId(null);
        return new UserId(Reader.ReadString());
    }
    public SessionId ReadSessionId(BinaryReader Reader)
    {
        return new SessionId(Reader.ReadString());
    }
    public SessionId? ReadNullableSessionId(BinaryReader Reader)
    {
        if (Reader.ReadBoolean()) return null;
        return new SessionId(Reader.ReadString());
    }
    public string ReadMessageData(BinaryReader Reader)
    {
        var messageData = Reader.ReadString();
        var messageEnd = Reader.ReadString();
        var messageValid = messageEnd == "[EndOfData]";
        if (!messageValid) throw new Exception("This looks like a buffer overflow hack. Please go away.");
        return messageData;
    }


    public void WriteMessageType(BinaryWriter w, ReceivedMessageType type)
    {
        w.Write((byte)type);
    }
    public void WriteServiceId(BinaryWriter w, ServiceId id)
    {
        w.Write(id.Value);
    }
    public void WriteUserId(BinaryWriter w, UserId id)
    {
        w.Write(id.Value == null);
        if (id.Value == null) return;
        w.Write(id.Value);
    }
    public void WriteNullableUserId(BinaryWriter w, UserId? id)
    {
        w.Write(id == null);
        if (id == null) return;
        w.Write(id?.Value == null);
        if (id?.Value == null) return;
        w.Write(id.Value.Value);
    }
    public void WriteSessionId(BinaryWriter w, SessionId id)
    {
        w.Write(id.Value);
    }
    public void WriteNullableSessionId(BinaryWriter w, SessionId? id)
    {
        w.Write(id == null);
        if (id == null) return; 
        w.Write(id.Value.Value);
    }
    public void WriteMessageData(BinaryWriter w, string data)
    {
        w.Write(data);
        w.Write("[EndOfData]");
    }

}