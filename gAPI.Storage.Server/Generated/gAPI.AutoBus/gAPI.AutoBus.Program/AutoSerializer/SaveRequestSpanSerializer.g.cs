using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Serializers;
using gAPI.Core.Server.Storage.StorageServer.Dtos.Requests;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Generated;

public static class SaveRequestSpanSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0x5C8359F9;
    public const uint SchemaHash = 0xF1741CBB;

    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> ___span, ref int ___offset, SaveRequest value)
    {
        PrimitivesSpanSerializer.WriteUShort(ref ___span, ref ___offset, Magic); // Magic string `GA` => it's a gAPI stream
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, TypeId); // Type identifier
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, SchemaHash); // Schema identifier
        
        PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.StorageKey);
        PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.FileName);
        PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.MimeType);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.AllowOverwrite);
    }

    [IsSpanSerializerRead]
    public static SaveRequest ReadSaveRequest(this ReadOnlySpan<byte> ___span, ref int ___offset)
    {
        var magicCheck = PrimitivesSpanSerializer.ReadUShort(___span, ref ___offset);// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        var value = new SaveRequest();
        value.StorageKey = PrimitivesSpanSerializer.ReadString(___span, ref ___offset);
        value.FileName = PrimitivesSpanSerializer.ReadString(___span, ref ___offset);
        value.MimeType = PrimitivesSpanSerializer.ReadString(___span, ref ___offset);
        value.AllowOverwrite = PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset);
        return value;
    }

    [IsSpanSerializerLength]
    public static int Length(ref int ___offset, SaveRequest value)
    {
        ___offset += 10;
        PrimitivesSpanSerializer.LengthString(ref ___offset, value.StorageKey);
        PrimitivesSpanSerializer.LengthString(ref ___offset, value.FileName);
        PrimitivesSpanSerializer.LengthString(ref ___offset, value.MimeType);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.AllowOverwrite);
        return ___offset;
    }
}