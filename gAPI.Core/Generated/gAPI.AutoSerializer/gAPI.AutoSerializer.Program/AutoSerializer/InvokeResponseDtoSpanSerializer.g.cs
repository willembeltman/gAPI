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

public static class InvokeResponseDtoSpanSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0xDB12BA9B;
    public const uint SchemaHash = 0x1568B443;

    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> ___span, ref int ___offset, InvokeResponseDto value)
    {
        PrimitivesSpanSerializer.WriteUShort(ref ___span, ref ___offset, Magic); // Magic string `GA` => it's a gAPI stream
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, TypeId); // Type identifier
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, SchemaHash); // Schema identifier
        
        SessionIdSpanSerializer.Write(ref ___span, ref ___offset, value.RespondingSessionId);
        RequestIdSpanSerializer.Write(ref ___span, ref ___offset, value.RequestId);
        ServiceIdSpanSerializer.Write(ref ___span, ref ___offset, value.ServiceId);
        ServiceMethodIdSpanSerializer.Write(ref ___span, ref ___offset, value.MethodId);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.UserId != null);
        if (value.UserId != null)
            UserIdSpanSerializer.Write(ref ___span, ref ___offset, value.UserId.Value);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.SessionId != null);
        if (value.SessionId != null)
            SessionIdSpanSerializer.Write(ref ___span, ref ___offset, value.SessionId.Value);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.StateIsChanged);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.StateData != null);
        if (value.StateData != null)
            PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.StateData);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.BinaryData != null);
        if (value.BinaryData != null)
        {
            PrimitivesSpanSerializer.WriteByteArray(ref ___span, ref ___offset, value.BinaryData);
        }
    }

    [IsSpanSerializerRead]
    public static InvokeResponseDto ReadInvokeResponseDto(this ReadOnlySpan<byte> ___span, ref int ___offset)
    {
        var magicCheck = PrimitivesSpanSerializer.ReadUShort(___span, ref ___offset);// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        return new InvokeResponseDto(SessionIdSpanSerializer.ReadSessionId(___span, ref ___offset), RequestIdSpanSerializer.ReadRequestId(___span, ref ___offset), ServiceIdSpanSerializer.ReadServiceId(___span, ref ___offset), ServiceMethodIdSpanSerializer.ReadServiceMethodId(___span, ref ___offset), PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : UserIdSpanSerializer.ReadUserId(___span, ref ___offset), PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : SessionIdSpanSerializer.ReadSessionId(___span, ref ___offset), PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset), PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : PrimitivesSpanSerializer.ReadString(___span, ref ___offset), PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : PrimitivesSpanSerializer.ReadByteArray(___span, ref ___offset));
    }

    [IsSpanSerializerLength]
    public static int Length(ref int ___offset, InvokeResponseDto value)
    {
        ___offset += 10;
        SessionIdSpanSerializer.Length(ref ___offset, value.RespondingSessionId);
        RequestIdSpanSerializer.Length(ref ___offset, value.RequestId);
        ServiceIdSpanSerializer.Length(ref ___offset, value.ServiceId);
        ServiceMethodIdSpanSerializer.Length(ref ___offset, value.MethodId);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.UserId != null);
        if (value.UserId != null)
            UserIdSpanSerializer.Length(ref ___offset, value.UserId.Value);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.SessionId != null);
        if (value.SessionId != null)
            SessionIdSpanSerializer.Length(ref ___offset, value.SessionId.Value);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.StateIsChanged);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.StateData != null);
        if (value.StateData != null)
            PrimitivesSpanSerializer.LengthString(ref ___offset, value.StateData);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.BinaryData != null);
        if (value.BinaryData != null)
        {
            PrimitivesSpanSerializer.LengthByteArray(ref ___offset, value.BinaryData);
        }
        return ___offset;
    }
}