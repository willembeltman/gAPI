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

public static class SynchronizeClientIdsDtoSpanSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0xB4A722BD;
    public const uint SchemaHash = 0xA78E3015;

    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> ___span, ref int ___offset, SynchronizeClientIdsDto value)
    {
        PrimitivesSpanSerializer.WriteUShort(ref ___span, ref ___offset, Magic); // Magic string `GA` => it's a gAPI stream
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, TypeId); // Type identifier
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, SchemaHash); // Schema identifier
        
        FabricManagerIdSpanSerializer.Write(ref ___span, ref ___offset, value.FabricManagerId);
        FabricConnectionIdSpanSerializer.Write(ref ___span, ref ___offset, value.FabricConnectionId);
        ClientConnectionIdSpanSerializer.Write(ref ___span, ref ___offset, value.ClientConnectionId);
    }

    [IsSpanSerializerRead]
    public static SynchronizeClientIdsDto ReadSynchronizeClientIdsDto(this ReadOnlySpan<byte> ___span, ref int ___offset)
    {
        var magicCheck = PrimitivesSpanSerializer.ReadUShort(___span, ref ___offset);// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        return new SynchronizeClientIdsDto(FabricManagerIdSpanSerializer.ReadFabricManagerId(___span, ref ___offset), FabricConnectionIdSpanSerializer.ReadFabricConnectionId(___span, ref ___offset), ClientConnectionIdSpanSerializer.ReadClientConnectionId(___span, ref ___offset));
    }

    [IsSpanSerializerLength]
    public static int Length(ref int ___offset, SynchronizeClientIdsDto value)
    {
        ___offset += 10;
        FabricManagerIdSpanSerializer.Length(ref ___offset, value.FabricManagerId);
        FabricConnectionIdSpanSerializer.Length(ref ___offset, value.FabricConnectionId);
        ClientConnectionIdSpanSerializer.Length(ref ___offset, value.ClientConnectionId);
        return ___offset;
    }
}