using gAPI.Core.Wss;
using gAPI.Core.Serializers;
using System.IO;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Attributes;

#nullable enable
namespace gAPI.Core.Wss;

public static class WssLoggerLogDtoSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0xE9E0DE67;
    public const uint SchemaHash = 0xE4D87B41;

    [IsSerializerWrite]
    public static void Write(this BinaryWriter ___writer, WssLoggerLogDto value)
    {
        ___writer.Write(Magic); // Magic string `GA` => it's a gAPI stream
        ___writer.Write(TypeId); // Type identifier
        ___writer.Write(SchemaHash); // Schema identifier
        
        ___writer.Write((int)value.Level);
        ___writer.Write(value.Message);
        ___writer.Write(value.Category != null); 
        if (value.Category != null)
            ___writer.Write(value.Category);
        ___writer.Write(value.Source != null); 
        if (value.Source != null)
            ___writer.Write(value.Source);
        ___writer.Write(value.Timestamp);
        ___writer.Write(value.CorrelationId != null); 
        if (value.CorrelationId != null)
            ___writer.Write(value.CorrelationId);
        ___writer.Write(value.UserId != null); 
        if (value.UserId != null)
            ___writer.Write(value.UserId);
        ___writer.Write(value.Data != null); 
        if (value.Data != null) 
        {
            ___writer.Write(value.Data.Length);
            foreach(var item1 in value.Data)
            {
                SignalRLogDataDtoSerializer.Write(___writer, item1);
            }
        }
        ___writer.Write(value.StackTrace != null); 
        if (value.StackTrace != null)
            ___writer.Write(value.StackTrace);
    }

    [IsSerializerRead]
    public static WssLoggerLogDto ReadWssLoggerLogDto(this BinaryReader ___reader)
    {
        var magicCheck = ___reader.ReadUInt16();// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = ___reader.ReadUInt32(); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = ___reader.ReadUInt32(); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        var value = new WssLoggerLogDto();
        value.Level = (Microsoft.Extensions.Logging.LogLevel)___reader.ReadInt32();
        value.Message = ___reader.ReadString();
        value.Category = ___reader.ReadBoolean() == false ? null : ___reader.ReadString();
        value.Source = ___reader.ReadBoolean() == false ? null : ___reader.ReadString();
        value.Timestamp = ___reader.ReadDateTimeOffset();
        value.CorrelationId = ___reader.ReadBoolean() == false ? null : ___reader.ReadString();
        value.UserId = ___reader.ReadBoolean() == false ? null : ___reader.ReadString();
        value.Data = ___reader.ReadBoolean() == false ? null : [.. Enumerable.Range(0, ___reader.ReadInt32()).Select(item => SignalRLogDataDtoSerializer.ReadSignalRLogDataDto(___reader))];
        value.StackTrace = ___reader.ReadBoolean() == false ? null : ___reader.ReadString();
        return value;
    }
}