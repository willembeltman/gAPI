using gAPI.Core.Dtos;
using gAPI.Core.Serializers;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

#nullable enable
namespace gAPI.Core.Dtos;

public static class AuthStateUserDtoSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0xCD8DF364;
    public const uint SchemaHash = 0x23E4A364;

    [IsSerializerWrite]
    public static void Write(this BinaryWriter ___writer, AuthStateUserDto value)
    {
        ___writer.Write(Magic); // Magic string `GA` => it's a gAPI stream
        ___writer.Write(TypeId); // Type identifier
        ___writer.Write(SchemaHash); // Schema identifier
        
        ___writer.Write(value.Id);
        ___writer.Write(value.UserName);
        ___writer.Write(value.Email);
    }

    [IsSerializerRead]
    public static AuthStateUserDto ReadAuthStateUserDto(this BinaryReader ___reader)
    {
        var magicCheck = ___reader.ReadUInt16();// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = ___reader.ReadUInt32(); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = ___reader.ReadUInt32(); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        var value = new AuthStateUserDto();
        value.Id = ___reader.ReadGuid();
        value.UserName = ___reader.ReadString();
        value.Email = ___reader.ReadString();
        return value;
    }
}