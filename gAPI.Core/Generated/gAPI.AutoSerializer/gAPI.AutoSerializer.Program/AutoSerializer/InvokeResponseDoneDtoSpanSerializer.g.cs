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

public static class InvokeResponseDoneDtoSpanSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0xBEB8CB23;
    public const uint SchemaHash = 0x99F3CF34;

    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> ___span, ref int ___offset, InvokeResponseDoneDto value)
    {
        PrimitivesSpanSerializer.WriteUShort(ref ___span, ref ___offset, Magic); // Magic string `GA` => it's a gAPI stream
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, TypeId); // Type identifier
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, SchemaHash); // Schema identifier
        
        RequestIdSpanSerializer.Write(ref ___span, ref ___offset, value.RequestId);
        ServiceIdSpanSerializer.Write(ref ___span, ref ___offset, value.ServiceId);
        ServiceMethodIdSpanSerializer.Write(ref ___span, ref ___offset, value.MethodId);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.SessionId != null);
        if (value.SessionId != null)
            SessionIdSpanSerializer.Write(ref ___span, ref ___offset, value.SessionId.Value);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.UserId != null);
        if (value.UserId != null)
            UserIdSpanSerializer.Write(ref ___span, ref ___offset, value.UserId.Value);
    }

    [IsSpanSerializerRead]
    public static InvokeResponseDoneDto ReadInvokeResponseDoneDto(this ReadOnlySpan<byte> ___span, ref int ___offset)
    {
        var magicCheck = PrimitivesSpanSerializer.ReadUShort(___span, ref ___offset);// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        var value = new InvokeResponseDoneDto();
        value.RequestId = RequestIdSpanSerializer.ReadRequestId(___span, ref ___offset);
        value.ServiceId = ServiceIdSpanSerializer.ReadServiceId(___span, ref ___offset);
        value.MethodId = ServiceMethodIdSpanSerializer.ReadServiceMethodId(___span, ref ___offset);
        value.SessionId = PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : SessionIdSpanSerializer.ReadSessionId(___span, ref ___offset);
        value.UserId = PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : UserIdSpanSerializer.ReadUserId(___span, ref ___offset);
        return value;
    }

    [IsSpanSerializerLength]
    public static int Length(ref int ___offset, InvokeResponseDoneDto value)
    {
        ___offset += 10;
        RequestIdSpanSerializer.Length(ref ___offset, value.RequestId);
        ServiceIdSpanSerializer.Length(ref ___offset, value.ServiceId);
        ServiceMethodIdSpanSerializer.Length(ref ___offset, value.MethodId);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.SessionId != null);
        if (value.SessionId != null)
            SessionIdSpanSerializer.Length(ref ___offset, value.SessionId.Value);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.UserId != null);
        if (value.UserId != null)
            UserIdSpanSerializer.Length(ref ___offset, value.UserId.Value);
        return ___offset;
    }
}