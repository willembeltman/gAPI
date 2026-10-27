using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Serializers;
using gAPI.Core.Server.Storage;
using gAPI.Core.Server.Storage.StorageServer.Dtos.Responses;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Generated;

public static class AppendResponseSpanSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0xAEA5592A;
    public const uint SchemaHash = 0x4DE6633E;

    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> ___span, ref int ___offset, AppendResponse value)
    {
        PrimitivesSpanSerializer.WriteUShort(ref ___span, ref ___offset, Magic); // Magic string `GA` => it's a gAPI stream
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, TypeId); // Type identifier
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, SchemaHash); // Schema identifier
        
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.Success);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.ErrorMessage.HasValue);
        if (value.ErrorMessage.HasValue)
            PrimitivesSpanSerializer.WriteInt32(ref ___span, ref ___offset, (int)value.ErrorMessage.Value);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.Url != null);
        if (value.Url != null)
            PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.Url);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.FileInfo != null);
        if (value.FileInfo != null)
            StorageFileInfoSpanSerializer.Write(ref ___span, ref ___offset, value.FileInfo);
    }

    [IsSpanSerializerRead]
    public static AppendResponse ReadAppendResponse(this ReadOnlySpan<byte> ___span, ref int ___offset)
    {
        var magicCheck = PrimitivesSpanSerializer.ReadUShort(___span, ref ___offset);// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        var value = new AppendResponse();
        value.Success = PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset);
        value.ErrorMessage = PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : (gAPI.Core.Server.Storage.StorageServer.Enums.ErrorMessagesEnum)PrimitivesSpanSerializer.ReadInt32(___span, ref ___offset);
        value.Url = PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : PrimitivesSpanSerializer.ReadString(___span, ref ___offset);
        value.FileInfo = PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : StorageFileInfoSpanSerializer.ReadStorageFileInfo(___span, ref ___offset);
        return value;
    }

    [IsSpanSerializerLength]
    public static int Length(ref int ___offset, AppendResponse value)
    {
        ___offset += 10;
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.Success);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.ErrorMessage.HasValue);
        if (value.ErrorMessage.HasValue)
            PrimitivesSpanSerializer.LengthInt32(ref ___offset, (int)value.ErrorMessage.Value);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.Url != null);
        if (value.Url != null)
            PrimitivesSpanSerializer.LengthString(ref ___offset, value.Url);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.FileInfo != null);
        if (value.FileInfo != null)
            StorageFileInfoSpanSerializer.Length(ref ___offset, value.FileInfo);
        return ___offset;
    }
}