using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

#nullable enable
namespace gAPI.Core.Dtos;

public static class StreamingResponseDtoSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0x170227ED;
    public const uint SchemaHash = 0xC4515737;

    [IsSerializerWrite]
    public static void Write(this BinaryWriter ___writer, StreamingResponseDto value)
    {
        ___writer.Write(Magic); // Magic string `GA` => it's a gAPI stream
        ___writer.Write(TypeId); // Type identifier
        ___writer.Write(SchemaHash); // Schema identifier
        
        SessionIdSerializer.Write(___writer, value.ResponseFromSessionId);
        RoutingDtoSerializer.Write(___writer, value.Routing);
        ___writer.Write(value.ArgumentIndex);
        StreamIdSerializer.Write(___writer, value.StreamId);
        ___writer.Write(value.IsCompleted);
        ___writer.Write(value.StateIsChanged);
        ___writer.Write(value.StateData != null); 
        if (value.StateData != null)
            ___writer.Write(value.StateData);
        ___writer.Write(value.BinaryData.Length);
        ___writer.Write(value.BinaryData);
    }

    [IsSerializerRead]
    public static StreamingResponseDto ReadStreamingResponseDto(this BinaryReader ___reader)
    {
        var magicCheck = ___reader.ReadUInt16();// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = ___reader.ReadUInt32(); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = ___reader.ReadUInt32(); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        return new StreamingResponseDto(SessionIdSerializer.ReadSessionId(___reader), RoutingDtoSerializer.ReadRoutingDto(___reader), ___reader.ReadInt32(), StreamIdSerializer.ReadStreamId(___reader), ___reader.ReadBoolean(), ___reader.ReadBoolean(), ___reader.ReadBoolean() == false ? null : ___reader.ReadString(), ___reader.ReadBytes(___reader.ReadInt32()));
    }
}