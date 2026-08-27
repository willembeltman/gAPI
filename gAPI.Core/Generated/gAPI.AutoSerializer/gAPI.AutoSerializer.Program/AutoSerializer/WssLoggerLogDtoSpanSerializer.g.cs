using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Serializers;
using gAPI.Core.Wss;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Wss;

public static class WssLoggerLogDtoSpanSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0xE9E0DE67;
    public const uint SchemaHash = 0xE4D87B41;

    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> ___span, ref int ___offset, WssLoggerLogDto value)
    {
        PrimitivesSpanSerializer.WriteUShort(ref ___span, ref ___offset, Magic); // Magic string `GA` => it's a gAPI stream
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, TypeId); // Type identifier
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, SchemaHash); // Schema identifier
        
        PrimitivesSpanSerializer.WriteInt32(ref ___span, ref ___offset, (int)value.Level);
        PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.Message);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.Category != null);
        if (value.Category != null)
            PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.Category);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.Source != null);
        if (value.Source != null)
            PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.Source);
        DateTimeOffsetSerializers.Write(ref ___span, ref ___offset, value.Timestamp);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.CorrelationId != null);
        if (value.CorrelationId != null)
            PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.CorrelationId);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.UserId != null);
        if (value.UserId != null)
            PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.UserId);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.Data != null);
        if (value.Data != null) 
        {
            PrimitivesSpanSerializer.WriteInt32(ref ___span, ref ___offset, value.Data.Length);
            foreach(var item1 in value.Data)
            {
                SignalRLogDataDtoSpanSerializer.Write(ref ___span, ref ___offset, item1);
            }
        }
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.StackTrace != null);
        if (value.StackTrace != null)
            PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.StackTrace);
    }

    [IsSpanSerializerRead]
    public static WssLoggerLogDto ReadWssLoggerLogDto(this ReadOnlySpan<byte> ___span, ref int ___offset)
    {
        var magicCheck = PrimitivesSpanSerializer.ReadUShort(___span, ref ___offset);// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        var value = new WssLoggerLogDto();
        value.Level = (Microsoft.Extensions.Logging.LogLevel)PrimitivesSpanSerializer.ReadInt32(___span, ref ___offset);
        value.Message = PrimitivesSpanSerializer.ReadString(___span, ref ___offset);
        value.Category = PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : PrimitivesSpanSerializer.ReadString(___span, ref ___offset);
        value.Source = PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : PrimitivesSpanSerializer.ReadString(___span, ref ___offset);
        value.Timestamp = DateTimeOffsetSerializers.ReadDateTimeOffset(___span, ref ___offset);
        value.CorrelationId = PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : PrimitivesSpanSerializer.ReadString(___span, ref ___offset);
        value.UserId = PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : PrimitivesSpanSerializer.ReadString(___span, ref ___offset);
        value.Data = PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : BuildListSignalRLogDataDto(___span, ref ___offset, PrimitivesSpanSerializer.ReadInt32(___span, ref ___offset));
        value.StackTrace = PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : PrimitivesSpanSerializer.ReadString(___span, ref ___offset);
        return value;
    }

    [IsSpanSerializerLength]
    public static int Length(ref int ___offset, WssLoggerLogDto value)
    {
        ___offset += 10;
        PrimitivesSpanSerializer.LengthInt32(ref ___offset, (int)value.Level);
        PrimitivesSpanSerializer.LengthString(ref ___offset, value.Message);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.Category != null);
        if (value.Category != null)
            PrimitivesSpanSerializer.LengthString(ref ___offset, value.Category);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.Source != null);
        if (value.Source != null)
            PrimitivesSpanSerializer.LengthString(ref ___offset, value.Source);
        DateTimeOffsetSerializers.Length(ref ___offset, value.Timestamp);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.CorrelationId != null);
        if (value.CorrelationId != null)
            PrimitivesSpanSerializer.LengthString(ref ___offset, value.CorrelationId);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.UserId != null);
        if (value.UserId != null)
            PrimitivesSpanSerializer.LengthString(ref ___offset, value.UserId);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.Data != null);
        if (value.Data != null) 
        {
            PrimitivesSpanSerializer.LengthInt32(ref ___offset, value.Data.Length);
            foreach(var item2 in value.Data)
            {
                SignalRLogDataDtoSpanSerializer.Length(ref ___offset, item2);
            }
        }
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.StackTrace != null);
        if (value.StackTrace != null)
            PrimitivesSpanSerializer.LengthString(ref ___offset, value.StackTrace);
        return ___offset;
    }

    static SignalRLogDataDto[] BuildListSignalRLogDataDto(ReadOnlySpan<byte> ___span, ref int ___offset, int count)
    {
        var list = new List<SignalRLogDataDto>(count);
        for (int i = 0; i < count; i++)
        {
            var item = SignalRLogDataDtoSpanSerializer.ReadSignalRLogDataDto(___span, ref ___offset);
            list.Add(item);
        }
        return [.. list];
    }
}