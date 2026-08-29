using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

#nullable enable
namespace gAPI.Core.Dtos;

public static class InvokeArgumentResponseDtoSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0x40CFF5FA;
    public const uint SchemaHash = 0x45353D15;

    [IsSerializerWrite]
    public static void Write(this BinaryWriter ___writer, InvokeArgumentResponseDto value)
    {
        ___writer.Write(Magic); // Magic string `GA` => it's a gAPI stream
        ___writer.Write(TypeId); // Type identifier
        ___writer.Write(SchemaHash); // Schema identifier
        
        RequestIdSerializer.Write(___writer, value.RequestId);
        ___writer.Write(value.ArgumentIndex);
        ___writer.Write(value.IsCompleted);
        ___writer.Write(value.BinaryData.Length);
        ___writer.Write(value.BinaryData);
    }

    [IsSerializerRead]
    public static InvokeArgumentResponseDto ReadInvokeArgumentResponseDto(this BinaryReader ___reader)
    {
        var magicCheck = ___reader.ReadUInt16();// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = ___reader.ReadUInt32(); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = ___reader.ReadUInt32(); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        var value = new InvokeArgumentResponseDto();
        value.RequestId = RequestIdSerializer.ReadRequestId(___reader);
        value.ArgumentIndex = ___reader.ReadInt32();
        value.IsCompleted = ___reader.ReadBoolean();
        value.BinaryData = ___reader.ReadBytes(___reader.ReadInt32());
        return value;
    }
}