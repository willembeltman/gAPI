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

public static class InvokeRequestDoneDtoSpanSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0x498DADF7;
    public const uint SchemaHash = 0xF77407BD;

    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> ___span, ref int ___offset, InvokeRequestDoneDto value)
    {
        PrimitivesSpanSerializer.WriteUShort(ref ___span, ref ___offset, Magic); // Magic string `GA` => it's a gAPI stream
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, TypeId); // Type identifier
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, SchemaHash); // Schema identifier
        
        RoutingDtoSpanSerializer.Write(ref ___span, ref ___offset, value.Routing);
        PrimitivesSpanSerializer.WriteInt32(ref ___span, ref ___offset, value.StreamIds.Length);
        foreach(var item1 in value.StreamIds)
        {
            StreamIdSpanSerializer.Write(ref ___span, ref ___offset, item1);
        }
    }

    [IsSpanSerializerRead]
    public static InvokeRequestDoneDto ReadInvokeRequestDoneDto(this ReadOnlySpan<byte> ___span, ref int ___offset)
    {
        var magicCheck = PrimitivesSpanSerializer.ReadUShort(___span, ref ___offset);// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        return new InvokeRequestDoneDto(RoutingDtoSpanSerializer.ReadRoutingDto(___span, ref ___offset), BuildListStreamId(___span, ref ___offset, PrimitivesSpanSerializer.ReadInt32(___span, ref ___offset)));
    }

    [IsSpanSerializerLength]
    public static int Length(ref int ___offset, InvokeRequestDoneDto value)
    {
        ___offset += 10;
        RoutingDtoSpanSerializer.Length(ref ___offset, value.Routing);
        PrimitivesSpanSerializer.LengthInt32(ref ___offset, value.StreamIds.Length);
        foreach(var item2 in value.StreamIds)
        {
            StreamIdSpanSerializer.Length(ref ___offset, item2);
        }
        return ___offset;
    }

    static StreamId[] BuildListStreamId(ReadOnlySpan<byte> ___span, ref int ___offset, int count)
    {
        var list = new List<StreamId>(count);
        for (int i = 0; i < count; i++)
        {
            var item = StreamIdSpanSerializer.ReadStreamId(___span, ref ___offset);
            list.Add(item);
        }
        return [.. list];
    }
}