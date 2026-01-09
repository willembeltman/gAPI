using gAPI.Ids;
using System;
using System.IO;
using System.Reflection.PortableExecutable;

namespace gAPI.Fabric;

public class FabricConverter
{
    public FabricClientToHostMessageEnum ReadClientToHostMessageType(BinaryReader Reader)
    {
        return (FabricClientToHostMessageEnum)Reader.ReadByte();
    }
    public FabricHostToClientMessageEnum ReadHostToClientMessageType(BinaryReader Reader)
    {
        return (FabricHostToClientMessageEnum)Reader.ReadByte();
    }
    public FabricHostId ReadFabricHostId(BinaryReader binaryReader)
    {
        return new FabricHostId(binaryReader.ReadInt64());
    }
    public SseServiceId ReadServiceId(BinaryReader Reader)
    {
        var serviceName = Reader.ReadString();
        var serviceId = new SseServiceId(serviceName);
        return serviceId;
    }
    public SseServiceMethodId ReadServiceMethodId(BinaryReader reader)
    {
        var serviceName = reader.ReadString();
        var serviceId = new SseServiceMethodId(serviceName);
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


    public void WriteClientToHostMessageType(BinaryWriter w, FabricClientToHostMessageEnum type)
    {
        w.Write((byte)type);
    }
    public void WriteHostToClientMessageType(BinaryWriter w, FabricHostToClientMessageEnum type)
    {
        w.Write((byte)type);
    }
    public void WriteFabricHostId(BinaryWriter w, FabricHostId id)
    {
        w.Write(id.Value);
    }
    public void WriteServiceId(BinaryWriter w, SseServiceId id)
    {
        w.Write(id.Value);
    }
    public void WriteServiceMethodId(BinaryWriter w, SseServiceMethodId id)
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