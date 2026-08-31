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

public static class InvokeArgumentRequestDtoSpanSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0xBD2B83B4;
    public const uint SchemaHash = 0x6819BAD2;

    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> ___span, ref int ___offset, InvokeArgumentRequestDto value)
    {
        PrimitivesSpanSerializer.WriteUShort(ref ___span, ref ___offset, Magic); // Magic string `GA` => it's a gAPI stream
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, TypeId); // Type identifier
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, SchemaHash); // Schema identifier
        
        RequestIdSpanSerializer.Write(ref ___span, ref ___offset, value.RequestId);
        PrimitivesSpanSerializer.WriteInt32(ref ___span, ref ___offset, value.ArgumentIndex);
        GuidSerializer.WriteGuid(ref ___span, ref ___offset, value.StreamId);
    }

    [IsSpanSerializerRead]
    public static InvokeArgumentRequestDto ReadInvokeArgumentRequestDto(this ReadOnlySpan<byte> ___span, ref int ___offset)
    {
        var magicCheck = PrimitivesSpanSerializer.ReadUShort(___span, ref ___offset);// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        var value = new InvokeArgumentRequestDto();
        value.RequestId = RequestIdSpanSerializer.ReadRequestId(___span, ref ___offset);
        value.ArgumentIndex = PrimitivesSpanSerializer.ReadInt32(___span, ref ___offset);
        value.StreamId = GuidSerializer.ReadGuid(___span, ref ___offset);
        return value;
    }

    [IsSpanSerializerLength]
    public static int Length(ref int ___offset, InvokeArgumentRequestDto value)
    {
        ___offset += 10;
        RequestIdSpanSerializer.Length(ref ___offset, value.RequestId);
        PrimitivesSpanSerializer.LengthInt32(ref ___offset, value.ArgumentIndex);
        GuidSerializer.GetMessageLength(ref ___offset, value.StreamId);
        return ___offset;
    }
}