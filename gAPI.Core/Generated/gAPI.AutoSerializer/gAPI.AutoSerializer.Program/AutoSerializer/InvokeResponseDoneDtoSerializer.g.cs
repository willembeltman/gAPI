using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

#nullable enable
namespace gAPI.Core.Dtos;

public static class InvokeResponseDoneDtoSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0xBEB8CB23;
    public const uint SchemaHash = 0x99F3CF34;

    [IsSerializerWrite]
    public static void Write(this BinaryWriter ___writer, InvokeResponseDoneDto value)
    {
        ___writer.Write(Magic); // Magic string `GA` => it's a gAPI stream
        ___writer.Write(TypeId); // Type identifier
        ___writer.Write(SchemaHash); // Schema identifier
        
        RequestIdSerializer.Write(___writer, value.RequestId);
        ServiceIdSerializer.Write(___writer, value.ServiceId);
        ServiceMethodIdSerializer.Write(___writer, value.MethodId);
        ___writer.Write(value.SessionId != null); 
        if (value.SessionId != null) 
            SessionIdSerializer.Write(___writer, value.SessionId.Value);
        ___writer.Write(value.UserId != null); 
        if (value.UserId != null) 
            UserIdSerializer.Write(___writer, value.UserId.Value);
    }

    [IsSerializerRead]
    public static InvokeResponseDoneDto ReadInvokeResponseDoneDto(this BinaryReader ___reader)
    {
        var magicCheck = ___reader.ReadUInt16();// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = ___reader.ReadUInt32(); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = ___reader.ReadUInt32(); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        var value = new InvokeResponseDoneDto();
        value.RequestId = RequestIdSerializer.ReadRequestId(___reader);
        value.ServiceId = ServiceIdSerializer.ReadServiceId(___reader);
        value.MethodId = ServiceMethodIdSerializer.ReadServiceMethodId(___reader);
        value.SessionId = ___reader.ReadBoolean() == false ? null : SessionIdSerializer.ReadSessionId(___reader);
        value.UserId = ___reader.ReadBoolean() == false ? null : UserIdSerializer.ReadUserId(___reader);
        return value;
    }
}