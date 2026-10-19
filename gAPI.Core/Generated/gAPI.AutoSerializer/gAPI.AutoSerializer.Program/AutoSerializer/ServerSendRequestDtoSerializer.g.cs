using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

#nullable enable
namespace gAPI.Core.Dtos;

public static class ServerSendRequestDtoSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0xAAAE3EDA;
    public const uint SchemaHash = 0x5A788975;

    [IsSerializerWrite]
    public static void Write(this BinaryWriter ___writer, ServerSendRequestDto value)
    {
        ___writer.Write(Magic); // Magic string `GA` => it's a gAPI stream
        ___writer.Write(TypeId); // Type identifier
        ___writer.Write(SchemaHash); // Schema identifier
        
        ServerRoutingDtoSerializer.Write(___writer, value.Routing);
        ___writer.Write(value.Path);
        ___writer.Write(value.QueryString != null); 
        if (value.QueryString != null)
            ___writer.Write(value.QueryString);
        ___writer.Write(value.IpAddress.Length);
        ___writer.Write(value.IpAddress);
        ___writer.Write(value.CookieData != null); 
        if (value.CookieData != null)
            ___writer.Write(value.CookieData);
        ___writer.Write(value.StateData != null); 
        if (value.StateData != null)
            ___writer.Write(value.StateData);
        ___writer.Write(value.SessionData != null); 
        if (value.SessionData != null)
            ___writer.Write(value.SessionData);
        ___writer.Write(value.BinaryData.Length);
        ___writer.Write(value.BinaryData);
    }

    [IsSerializerRead]
    public static ServerSendRequestDto ReadServerSendRequestDto(this BinaryReader ___reader)
    {
        var magicCheck = ___reader.ReadUInt16();// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = ___reader.ReadUInt32(); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = ___reader.ReadUInt32(); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        return new ServerSendRequestDto(ServerRoutingDtoSerializer.ReadServerRoutingDto(___reader), ___reader.ReadString(), ___reader.ReadBoolean() == false ? null : ___reader.ReadString(), ___reader.ReadBytes(___reader.ReadInt32()), ___reader.ReadBoolean() == false ? null : ___reader.ReadString(), ___reader.ReadBoolean() == false ? null : ___reader.ReadString(), ___reader.ReadBoolean() == false ? null : ___reader.ReadString(), ___reader.ReadBytes(___reader.ReadInt32()));
    }
}