using gAPI.Core.AttributesSerializers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Net.Http.Headers;
using System.Text;

namespace gAPI.Core.Serializers;

public static class IFormFileSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0x13370001;
    public const uint SchemaHash = 0x13370001;

    [IsSerializerWrite]
    public static void Write(this BinaryWriter ___writer, IFormFile formFile)
    {
        ___writer.Write(Magic); // Magic string `GA` => it's a gAPI stream
        ___writer.Write(TypeId); // Type identifier
        ___writer.Write(SchemaHash); // Schema identifier

        ___writer.Write(formFile.Headers.Keys.Count);
        foreach (var key in formFile.Headers.Keys)
        {
            var value = formFile.Headers[key];
            ___writer.Write(key != null);
            if (key != null)
                ___writer.Write(key);
            ___writer.Write(value.ToString());
        }
        ___writer.Write(formFile.ContentType);
        ___writer.Write(formFile.ContentDisposition);
        ___writer.Write(formFile.Name);
        ___writer.Write(formFile.FileName);

        using var stream = formFile.OpenReadStream();
        ___writer.Write(stream.Length);
        stream.CopyTo(___writer.BaseStream);
    }

    [IsSerializerRead]
    public static IFormFile ReadIFormFile(this BinaryReader ___reader)
    {
        var magicCheck = ___reader.ReadUInt16();// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = ___reader.ReadUInt32(); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = ___reader.ReadUInt32(); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");

        var obj = new ServerFormFile();
        var keyCount = ___reader.ReadInt32();
        for (int i = 0; i < keyCount; i++)
        {
            var key = ___reader.ReadBoolean() ? ___reader.ReadString() : null;
            var value = ___reader.ReadString();
            obj.Headers[key] = new StringValues(value);
        }
        obj.ContentType = ___reader.ReadString();
        obj.ContentDisposition = ___reader.ReadString();
        obj.Name = ___reader.ReadString();
        obj.FileName = ___reader.ReadString();

        obj.Buffer = ___reader.ReadBytes(___reader.ReadInt32());
        obj.Length = obj.Buffer.Length;
        return obj;
    }


    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> span, ref int offset, IFormFile formFile)
    {
        PrimitivesSpanSerializer.WriteUShort(ref span, ref offset, Magic); // Magic string `GA` => it's a gAPI stream
        PrimitivesSpanSerializer.WriteUInt(ref span, ref offset, TypeId); // Type identifier
        PrimitivesSpanSerializer.WriteUInt(ref span, ref offset, SchemaHash); // Schema identifier

        PrimitivesSpanSerializer.WriteInt32(ref span, ref offset, formFile.Headers.Keys.Count);
        foreach (var key in formFile.Headers.Keys)
        {
            var value = formFile.Headers[key];

            PrimitivesSpanSerializer.WriteBoolean(ref span, ref offset, key != null);
            if (key != null)
                PrimitivesSpanSerializer.WriteString(ref span, ref offset, key);
            PrimitivesSpanSerializer.WriteString(ref span, ref offset, value.ToString());
        }

        PrimitivesSpanSerializer.WriteString(ref span, ref offset, formFile.ContentType);
        PrimitivesSpanSerializer.WriteString(ref span, ref offset, formFile.ContentDisposition);
        PrimitivesSpanSerializer.WriteString(ref span, ref offset, formFile.Name);
        PrimitivesSpanSerializer.WriteString(ref span, ref offset, formFile.FileName);

        using var stream = formFile.OpenReadStream();
        if (stream is MemoryStream ms && ms.TryGetBuffer(out var arraySegment))
        {
            // Gelukt! Directe toegang tot de buffer zonder UnauthorizedAccessException
            var memory = arraySegment.AsMemory(0, (int)ms.Length);
            PrimitivesSpanSerializer.WriteByteReadOnlyMemory(ref span, ref offset, memory);
        }
        else
        {
            // Fallback: Als GetBuffer niet mag OF als het een ander type stream is.
            // Sinds .NET 7 gebruikt stream.ReadExactly de interne ReadOnlySpan optimalisaties.
            var buffer = new byte[stream.Length];
            stream.ReadExactly(buffer);
            PrimitivesSpanSerializer.WriteByteArray(ref span, ref offset, buffer);
        }

    }

    [IsSpanSerializerRead]
    public static IFormFile ReadIFormFile(this ReadOnlySpan<byte> span, ref int offset)
    {
        var magicCheck = PrimitivesSpanSerializer.ReadUShort(span, ref offset);// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = PrimitivesSpanSerializer.ReadUInt(span, ref offset); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = PrimitivesSpanSerializer.ReadUInt(span, ref offset); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");

        var obj = new ServerFormFile();
        var keyCount = PrimitivesSpanSerializer.ReadInt32(span, ref offset);
        for (int i = 0; i < keyCount; i++)
        {
            var key = PrimitivesSpanSerializer.ReadBoolean(span, ref offset) == false ? null : PrimitivesSpanSerializer.ReadString(span, ref offset);
            var value = PrimitivesSpanSerializer.ReadString(span, ref offset);
            obj.Headers[key] = new StringValues(value);
        }

        obj.ContentType = PrimitivesSpanSerializer.ReadString(span, ref offset);
        obj.ContentDisposition = PrimitivesSpanSerializer.ReadString(span, ref offset);
        obj.Name = PrimitivesSpanSerializer.ReadString(span, ref offset);
        obj.FileName = PrimitivesSpanSerializer.ReadString(span, ref offset);

        obj.Buffer = PrimitivesSpanSerializer.ReadByteArray(span, ref offset);
        obj.Length = obj.Buffer.Length;
        return obj;
    }

    [IsSpanSerializerLength]
    public static int GetMessageLength(ref int offset, IFormFile formFile)
    {
        offset += 2;
        offset += 4;
        offset += 4;

        offset += 4;
        foreach (var key in formFile.Headers.Keys)
        {
            var value = formFile.Headers[key];

            offset += 1;
            if (key != null)
            {
                offset += Encoding.UTF8.GetByteCount(key);
            }
            offset += Encoding.UTF8.GetByteCount(value.ToString());
        }

        offset += 4;
        offset += Encoding.UTF8.GetByteCount(formFile.ContentType);
        offset += 4;
        offset += Encoding.UTF8.GetByteCount(formFile.ContentDisposition);
        offset += 4;
        offset += Encoding.UTF8.GetByteCount(formFile.Name);
        offset += 4;
        offset += Encoding.UTF8.GetByteCount(formFile.FileName);

        offset += 4;
        offset += Convert.ToInt32(formFile.Length);
        return offset;
    }

    [IsMultipartFormDataContentSerializer]
    public static void Write(MultipartFormDataContent content, string propertyName, IFormFile value)
    {
        var fileContent = new StreamContent(value.OpenReadStream());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(value.ContentType);
        content.Add(fileContent, "file", value.FileName);
    }
}

public class ServerFormFile : IFormFile
{
    public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
    public string ContentType { get; set; } = string.Empty;
    public string ContentDisposition { get; set; } = string.Empty;
    public long Length { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public byte[] Buffer { get; set; } = [];

    public Stream OpenReadStream()
    {
        return new MemoryStream(Buffer);
    }

    public void CopyTo(Stream target)
    {
        target.Write(Buffer, 0, Buffer.Length);
    }

    public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
    {
        await target.WriteAsync(Buffer, 0, Buffer.Length);
    }
}