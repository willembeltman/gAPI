using gAPI.Core.Server.Enums;
using gAPI.Core.Ids;

namespace gAPI.Core.Server.Fabric;

public class FabricConverter
{
    public static FabricClientToHostMessageEnum ReadClientToHostMessageType(BinaryReader Reader) 
        => (FabricClientToHostMessageEnum)Reader.ReadByte();
    public static FabricHostToClientMessageEnum ReadHostToClientMessageType(BinaryReader Reader) 
        => (FabricHostToClientMessageEnum)Reader.ReadByte();
    public static FabricHostId ReadFabricHostId(BinaryReader binaryReader)
        => new(binaryReader.ReadInt64());

    public static void WriteClientToHostMessageType(BinaryWriter w, FabricClientToHostMessageEnum type)
        => w.Write((byte)type);
    public static void WriteHostToClientMessageType(BinaryWriter w, FabricHostToClientMessageEnum type)
        => w.Write((byte)type);
    public static void WriteFabricHostId(BinaryWriter w, FabricHostId id) 
        => w.Write(id.Value);

    //public ServiceId ReadServiceId(BinaryReader Reader)
    //{
    //    var value = Reader.ReadString();
    //    var messageEnd = Reader.ReadString();
    //    var messageValid = messageEnd == "[EndOfData]";
    //    if (!messageValid) throw new Exception("This looks like a buffer overflow hack. Please go away.");
    //    var serviceId = new ServiceId(value);
    //    return serviceId;
    //}
    //public ServiceMethodId ReadServiceMethodId(BinaryReader Reader)
    //{
    //    var value = Reader.ReadString();
    //    var messageEnd = Reader.ReadString();
    //    var messageValid = messageEnd == "[EndOfData]";
    //    if (!messageValid) throw new Exception("This looks like a buffer overflow hack. Please go away.");
    //    var serviceId = new ServiceMethodId(value);
    //    return serviceId;
    //}
    //public UserId ReadUserId(BinaryReader Reader)
    //{
    //    if (Reader.ReadBoolean()) return new UserId(null);
    //    var value = Reader.ReadString();
    //    var messageEnd = Reader.ReadString();
    //    var messageValid = messageEnd == "[EndOfData]";
    //    if (!messageValid) throw new Exception("This looks like a buffer overflow hack. Please go away.");
    //    return new UserId(value);
    //}
    //public UserId? ReadNullableUserId(BinaryReader Reader)
    //{
    //    if (Reader.ReadBoolean()) return null;
    //    if (Reader.ReadBoolean()) return new UserId(null);
    //    var value = Reader.ReadString();
    //    var messageEnd = Reader.ReadString();
    //    var messageValid = messageEnd == "[EndOfData]";
    //    if (!messageValid) throw new Exception("This looks like a buffer overflow hack. Please go away.");
    //    return new UserId(value);
    //}
    //public SessionId ReadSessionId(BinaryReader Reader)
    //{
    //    var value = Reader.ReadString();
    //    var messageEnd = Reader.ReadString();
    //    var messageValid = messageEnd == "[EndOfData]";
    //    if (!messageValid) throw new Exception("This looks like a buffer overflow hack. Please go away.");
    //    return new SessionId(value);
    //}
    //public SessionId? ReadNullableSessionId(BinaryReader Reader)
    //{
    //    if (Reader.ReadBoolean()) return null;
    //    var value = Reader.ReadString();
    //    var messageEnd = Reader.ReadString();
    //    var messageValid = messageEnd == "[EndOfData]";
    //    if (!messageValid) throw new Exception("This looks like a buffer overflow hack. Please go away.");
    //    return new SessionId(value);
    //}
    //public RequestId ReadRequestId(BinaryReader Reader)
    //{
    //    var value = Reader.ReadString();
    //    var messageEnd = Reader.ReadString();
    //    var messageValid = messageEnd == "[EndOfData]";
    //    if (!messageValid) throw new Exception("This looks like a buffer overflow hack. Please go away.");
    //    return new RequestId(value);
    //}
    //public string ReadMessageData(BinaryReader Reader)
    //{
    //    var messageData = Reader.ReadString();
    //    var messageEnd = Reader.ReadString();
    //    var messageValid = messageEnd == "[EndOfData]";
    //    if (!messageValid) throw new Exception("This looks like a buffer overflow hack. Please go away.");
    //    return messageData;
    //}
    //public string[] ReadResponsesData(BinaryReader Reader)
    //{
    //    List<string> responses = [];
    //    var length = Reader.ReadInt32();
    //    for (int i = 0; i < length; i++)
    //    {
    //        var response = Reader.ReadString();
    //        responses.Add(response);
    //    }
    //    var messageEnd = Reader.ReadString();
    //    var messageValid = messageEnd == "[EndOfData]";
    //    if (!messageValid) throw new Exception("This looks like a buffer overflow hack. Please go away.");
    //    return [.. responses];
    //}
    //public string ReadResponseData(BinaryReader Reader)
    //{
    //    var messageData = Reader.ReadString();
    //    var messageEnd = Reader.ReadString();
    //    var messageValid = messageEnd == "[EndOfData]";
    //    if (!messageValid) throw new Exception("This looks like a buffer overflow hack. Please go away.");
    //    return messageData;
    //}




    //public void WriteServiceId(BinaryWriter w, ServiceId id)
    //{
    //    w.Write(id.Value);
    //    w.Write("[EndOfData]");
    //}
    //public void WriteServiceMethodId(BinaryWriter w, ServiceMethodId id)
    //{
    //    w.Write(id.Value);
    //    w.Write("[EndOfData]");
    //}
    //public void WriteUserId(BinaryWriter w, UserId id)
    //{
    //    w.Write(id.Value == null);
    //    if (id.Value == null) return;
    //    w.Write(id.Value);
    //    w.Write("[EndOfData]");
    //}
    //public void WriteNullableUserId(BinaryWriter w, UserId? id)
    //{
    //    w.Write(id == null);
    //    if (id == null) return;
    //    w.Write(id?.Value == null);
    //    if (id?.Value == null) return;
    //    w.Write(id.Value.Value);
    //    w.Write("[EndOfData]");
    //}
    //public void WriteSessionId(BinaryWriter w, SessionId id)
    //{
    //    w.Write(id.Value);
    //}
    //public void WriteNullableSessionId(BinaryWriter w, SessionId? id)
    //{
    //    w.Write(id == null);
    //    if (id == null) return;
    //    w.Write(id.Value.Value);
    //    w.Write("[EndOfData]");
    //}
    //public void WriteRequestId(BinaryWriter w, RequestId id)
    //{
    //    w.Write(id.Value);
    //    w.Write("[EndOfData]");
    //}
    //public void WriteMessageData(BinaryWriter w, string data)
    //{
    //    w.Write(data);
    //    w.Write("[EndOfData]");
    //}
    //public void WriteResponsesData(BinaryWriter w, string[] responses)
    //{
    //    w.Write(responses.Length);
    //    foreach(var response in responses) 
    //        w.Write(response);
    //    w.Write("[EndOfData]");
    //}
    //public void WriteResponseData(BinaryWriter w, string response)
    //{
    //    w.Write(response);
    //    w.Write("[EndOfData]");
    //}
}