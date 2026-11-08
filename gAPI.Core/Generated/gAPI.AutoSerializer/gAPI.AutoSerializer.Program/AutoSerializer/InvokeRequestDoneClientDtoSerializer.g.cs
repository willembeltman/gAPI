using gAPI.Core.Dtos;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

#nullable enable
namespace gAPI.Core.Dtos;

public static class InvokeRequestDoneClientDtoSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0x6E9BF33A;
    public const uint SchemaHash = 0x5FE36AFB;

    [IsSerializerWrite]
    public static void Write(this BinaryWriter ___writer, InvokeRequestDoneClientDto value)
    {
        ___writer.Write(Magic); // Magic string `GA` => it's a gAPI stream
        ___writer.Write(TypeId); // Type identifier
        ___writer.Write(SchemaHash); // Schema identifier
        
        RoutingDtoSerializer.Write(___writer, value.Routing);
        ___writer.Write(value.Cancelled);
        ___writer.Write(value.ExceptionMessage != null); 
        if (value.ExceptionMessage != null)
            ___writer.Write(value.ExceptionMessage);
        ___writer.Write(value.StateIsChanged);
        ___writer.Write(value.StateData != null); 
        if (value.StateData != null)
            ___writer.Write(value.StateData);
    }

    [IsSerializerRead]
    public static InvokeRequestDoneClientDto ReadInvokeRequestDoneClientDto(this BinaryReader ___reader)
    {
        var magicCheck = ___reader.ReadUInt16();// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = ___reader.ReadUInt32(); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = ___reader.ReadUInt32(); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        return new InvokeRequestDoneClientDto(RoutingDtoSerializer.ReadRoutingDto(___reader), ___reader.ReadBoolean(), ___reader.ReadBoolean() == false ? null : ___reader.ReadString(), ___reader.ReadBoolean(), ___reader.ReadBoolean() == false ? null : ___reader.ReadString());
    }
}