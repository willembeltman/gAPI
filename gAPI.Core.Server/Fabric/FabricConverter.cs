using gAPI.Core.Server.Enums;

namespace gAPI.Core.Server.Fabric;

public class FabricConverter
{
    public static FabricClientToHostMessageEnum ReadClientToHostMessageType(BinaryReader Reader)
        => (FabricClientToHostMessageEnum)Reader.ReadByte();
    public static FabricHostToClientMessageEnum ReadHostToClientMessageType(BinaryReader Reader)
        => (FabricHostToClientMessageEnum)Reader.ReadByte();

    public static void WriteClientToHostMessageType(BinaryWriter w, FabricClientToHostMessageEnum type)
        => w.Write((byte)type);
    public static void WriteHostToClientMessageType(BinaryWriter w, FabricHostToClientMessageEnum type)
        => w.Write((byte)type);
}

public class BusFabricConverter
{
    public static BusFabricClientToHostMessageEnum ReadClientToHostMessageType(BinaryReader Reader)
        => (BusFabricClientToHostMessageEnum)Reader.ReadByte();
    public static BusFabricHostToClientMessageEnum ReadHostToClientMessageType(BinaryReader Reader)
        => (BusFabricHostToClientMessageEnum)Reader.ReadByte();

    public static void WriteClientToHostMessageType(BinaryWriter w, BusFabricClientToHostMessageEnum type)
        => w.Write((byte)type);
    public static void WriteHostToClientMessageType(BinaryWriter w, BusFabricHostToClientMessageEnum type)
        => w.Write((byte)type);
}