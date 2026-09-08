using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Dtos;
using gAPI.Core.Serializers;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Dtos;

public static class InvokeRequestClientDtoSpanSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0x9A21F782;
    public const uint SchemaHash = 0x50D5250B;

    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> ___span, ref int ___offset, InvokeRequestClientDto value)
    {
        PrimitivesSpanSerializer.WriteUShort(ref ___span, ref ___offset, Magic); // Magic string `GA` => it's a gAPI stream
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, TypeId); // Type identifier
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, SchemaHash); // Schema identifier
        
        RoutingDtoSpanSerializer.Write(ref ___span, ref ___offset, value.Routing);
        PrimitivesSpanSerializer.WriteByteArray(ref ___span, ref ___offset, value.BinaryData);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.StateIsChanged);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.StateData != null);
        if (value.StateData != null)
            PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.StateData);
    }

    [IsSpanSerializerRead]
    public static InvokeRequestClientDto ReadInvokeRequestClientDto(this ReadOnlySpan<byte> ___span, ref int ___offset)
    {
        var magicCheck = PrimitivesSpanSerializer.ReadUShort(___span, ref ___offset);// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        return new InvokeRequestClientDto(RoutingDtoSpanSerializer.ReadRoutingDto(___span, ref ___offset), PrimitivesSpanSerializer.ReadByteArray(___span, ref ___offset), PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset), PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : PrimitivesSpanSerializer.ReadString(___span, ref ___offset));
    }

    [IsSpanSerializerLength]
    public static int Length(ref int ___offset, InvokeRequestClientDto value)
    {
        ___offset += 10;
        RoutingDtoSpanSerializer.Length(ref ___offset, value.Routing);
        PrimitivesSpanSerializer.LengthByteArray(ref ___offset, value.BinaryData);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.StateIsChanged);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.StateData != null);
        if (value.StateData != null)
            PrimitivesSpanSerializer.LengthString(ref ___offset, value.StateData);
        return ___offset;
    }
}