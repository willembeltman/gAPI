using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Serializers;
using gAPI.Core.Wss;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Wss;

public static class SignalRLogDataDtoSpanSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0xF40AB176;
    public const uint SchemaHash = 0xBFDA4C28;

    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> ___span, ref int ___offset, SignalRLogDataDto value)
    {
        PrimitivesSpanSerializer.WriteUShort(ref ___span, ref ___offset, Magic); // Magic string `GA` => it's a gAPI stream
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, TypeId); // Type identifier
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, SchemaHash); // Schema identifier
        
        PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.Key);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.Value != null);
        if (value.Value != null)
            PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.Value);
    }

    [IsSpanSerializerRead]
    public static SignalRLogDataDto ReadSignalRLogDataDto(this ReadOnlySpan<byte> ___span, ref int ___offset)
    {
        var magicCheck = PrimitivesSpanSerializer.ReadUShort(___span, ref ___offset);// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        var value = new SignalRLogDataDto();
        value.Key = PrimitivesSpanSerializer.ReadString(___span, ref ___offset);
        value.Value = PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : PrimitivesSpanSerializer.ReadString(___span, ref ___offset);
        return value;
    }

    [IsSpanSerializerLength]
    public static int Length(ref int ___offset, SignalRLogDataDto value)
    {
        ___offset += 10;
        PrimitivesSpanSerializer.LengthString(ref ___offset, value.Key);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.Value != null);
        if (value.Value != null)
            PrimitivesSpanSerializer.LengthString(ref ___offset, value.Value);
        return ___offset;
    }
}