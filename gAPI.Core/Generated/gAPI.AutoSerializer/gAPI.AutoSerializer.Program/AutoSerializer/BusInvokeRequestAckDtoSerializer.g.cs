using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

#nullable enable
namespace gAPI.Core.Dtos;

public static class BusInvokeRequestAckDtoSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0xBD364A5C;
    public const uint SchemaHash = 0x5277C94F;

    [IsSerializerWrite]
    public static void Write(this BinaryWriter ___writer, BusInvokeRequestAckDto value)
    {
        ___writer.Write(Magic); // Magic string `GA` => it's a gAPI stream
        ___writer.Write(TypeId); // Type identifier
        ___writer.Write(SchemaHash); // Schema identifier
        
        BusRoutingDtoSerializer.Write(___writer, value.Routing);
        ___writer.Write(value.Exception != null); 
        if (value.Exception != null)
            ___writer.Write(value.Exception);
    }

    [IsSerializerRead]
    public static BusInvokeRequestAckDto ReadBusInvokeRequestAckDto(this BinaryReader ___reader)
    {
        var magicCheck = ___reader.ReadUInt16();// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = ___reader.ReadUInt32(); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = ___reader.ReadUInt32(); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        return new BusInvokeRequestAckDto(BusRoutingDtoSerializer.ReadBusRoutingDto(___reader), ___reader.ReadBoolean() == false ? null : ___reader.ReadString());
    }
}