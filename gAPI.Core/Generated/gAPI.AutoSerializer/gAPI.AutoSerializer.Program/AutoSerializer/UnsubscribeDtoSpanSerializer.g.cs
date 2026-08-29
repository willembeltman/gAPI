using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Dtos;
using gAPI.Core.Ids;
using gAPI.Core.Serializers;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Dtos;

public static class UnsubscribeDtoSpanSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0x5E9420EB;
    public const uint SchemaHash = 0xAA6F3895;

    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> ___span, ref int ___offset, UnsubscribeDto value)
    {
        PrimitivesSpanSerializer.WriteUShort(ref ___span, ref ___offset, Magic); // Magic string `GA` => it's a gAPI stream
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, TypeId); // Type identifier
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, SchemaHash); // Schema identifier
        
        ServiceIdSpanSerializer.Write(ref ___span, ref ___offset, value.ServiceId);
        UserIdSpanSerializer.Write(ref ___span, ref ___offset, value.UserId);
        SessionIdSpanSerializer.Write(ref ___span, ref ___offset, value.SessionId);
    }

    [IsSpanSerializerRead]
    public static UnsubscribeDto ReadUnsubscribeDto(this ReadOnlySpan<byte> ___span, ref int ___offset)
    {
        var magicCheck = PrimitivesSpanSerializer.ReadUShort(___span, ref ___offset);// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        var value = new UnsubscribeDto();
        value.ServiceId = ServiceIdSpanSerializer.ReadServiceId(___span, ref ___offset);
        value.UserId = UserIdSpanSerializer.ReadUserId(___span, ref ___offset);
        value.SessionId = SessionIdSpanSerializer.ReadSessionId(___span, ref ___offset);
        return value;
    }

    [IsSpanSerializerLength]
    public static int Length(ref int ___offset, UnsubscribeDto value)
    {
        ___offset += 10;
        ServiceIdSpanSerializer.Length(ref ___offset, value.ServiceId);
        UserIdSpanSerializer.Length(ref ___offset, value.UserId);
        SessionIdSpanSerializer.Length(ref ___offset, value.SessionId);
        return ___offset;
    }
}