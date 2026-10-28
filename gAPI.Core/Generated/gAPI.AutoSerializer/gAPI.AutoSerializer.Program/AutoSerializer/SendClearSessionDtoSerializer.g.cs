using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

#nullable enable
namespace gAPI.Core.Dtos;

public static class SendClearSessionDtoSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0x2658E411;
    public const uint SchemaHash = 0x0E921B3D;

    [IsSerializerWrite]
    public static void Write(this BinaryWriter ___writer, SendClearSessionDto value)
    {
        ___writer.Write(Magic); // Magic string `GA` => it's a gAPI stream
        ___writer.Write(TypeId); // Type identifier
        ___writer.Write(SchemaHash); // Schema identifier
        
        SessionIdSerializer.Write(___writer, value.SessionId);
    }

    [IsSerializerRead]
    public static SendClearSessionDto ReadSendClearSessionDto(this BinaryReader ___reader)
    {
        var magicCheck = ___reader.ReadUInt16();// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = ___reader.ReadUInt32(); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = ___reader.ReadUInt32(); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        return new SendClearSessionDto(SessionIdSerializer.ReadSessionId(___reader));
    }
}