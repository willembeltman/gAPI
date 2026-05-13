using gAPI.Core.Enums;
using gAPI.Core.Ids;

namespace gAPI.Core.Server.Fabric;

public class WssConverter
{
    public static WssClientToServerMessageEnum ReadClientToHostMessageType(BinaryReader Reader)
        => (WssClientToServerMessageEnum)Reader.ReadByte();
    public static WssServerToClientMessageEnum ReadHostToClientMessageType(BinaryReader Reader)
        => (WssServerToClientMessageEnum)Reader.ReadByte();
    public static ConnectionId ReadConnectionId(BinaryReader binaryReader)
        => new(binaryReader.ReadInt64());

    public static void WriteClientToHostMessageType(BinaryWriter w, WssClientToServerMessageEnum type)
        => w.Write((byte)type);
    public static void WriteServerToClientMessageType(BinaryWriter w, WssServerToClientMessageEnum type)
        => w.Write((byte)type);
    public static void WriteConnectionId(BinaryWriter w, ConnectionId id)
        => w.Write(id.Value);
}