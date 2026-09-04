using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

#nullable enable
namespace gAPI.Core.Dtos;

public static class InvokeRequestDoneDtoSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0x498DADF7;
    public const uint SchemaHash = 0xF77407BD;

    [IsSerializerWrite]
    public static void Write(this BinaryWriter ___writer, InvokeRequestDoneDto value)
    {
        ___writer.Write(Magic); // Magic string `GA` => it's a gAPI stream
        ___writer.Write(TypeId); // Type identifier
        ___writer.Write(SchemaHash); // Schema identifier
        
        RoutingDtoSerializer.Write(___writer, value.Routing);
        ___writer.Write(value.StreamIds.Length);
        foreach(var item1 in value.StreamIds)
        {
            StreamIdSerializer.Write(___writer, item1);
        }
    }

    [IsSerializerRead]
    public static InvokeRequestDoneDto ReadInvokeRequestDoneDto(this BinaryReader ___reader)
    {
        var magicCheck = ___reader.ReadUInt16();// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = ___reader.ReadUInt32(); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = ___reader.ReadUInt32(); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        return new InvokeRequestDoneDto(RoutingDtoSerializer.ReadRoutingDto(___reader), [.. Enumerable.Range(0, ___reader.ReadInt32()).Select(item => StreamIdSerializer.ReadStreamId(___reader))]);
    }
}