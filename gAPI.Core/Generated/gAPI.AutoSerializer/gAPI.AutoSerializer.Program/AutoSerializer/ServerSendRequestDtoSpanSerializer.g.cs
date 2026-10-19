using gAPI.Core.Attributes;
using gAPI.Core.AttributesSerializers;
using gAPI.Core.Dtos;
using gAPI.Core.Serializers;
using System;
using System.Buffers.Binary;
using System.Text;

#nullable enable
namespace gAPI.Core.Dtos;

public static class ServerSendRequestDtoSpanSerializer
{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0xAAAE3EDA;
    public const uint SchemaHash = 0x5A788975;

    [IsSpanSerializerWrite]
    public static void Write(this ref Span<byte> ___span, ref int ___offset, ServerSendRequestDto value)
    {
        PrimitivesSpanSerializer.WriteUShort(ref ___span, ref ___offset, Magic); // Magic string `GA` => it's a gAPI stream
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, TypeId); // Type identifier
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, SchemaHash); // Schema identifier
        
        ServerRoutingDtoSpanSerializer.Write(ref ___span, ref ___offset, value.Routing);
        PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.Path);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.QueryString != null);
        if (value.QueryString != null)
            PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.QueryString);
        PrimitivesSpanSerializer.WriteByteArray(ref ___span, ref ___offset, value.IpAddress);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.CookieData != null);
        if (value.CookieData != null)
            PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.CookieData);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.StateData != null);
        if (value.StateData != null)
            PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.StateData);
        PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, value.SessionData != null);
        if (value.SessionData != null)
            PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, value.SessionData);
        PrimitivesSpanSerializer.WriteByteArray(ref ___span, ref ___offset, value.BinaryData);
    }

    [IsSpanSerializerRead]
    public static ServerSendRequestDto ReadServerSendRequestDto(this ReadOnlySpan<byte> ___span, ref int ___offset)
    {
        var magicCheck = PrimitivesSpanSerializer.ReadUShort(___span, ref ___offset);// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($"magic does not match, expected: `0x{Magic:X4}`, got: `0x{magicCheck:X4}`");
        var typeIdCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($"TypeIdCheck does not match, expected: `0x{TypeId:X8}`, got: `0x{typeIdCheck:X8}`");
        var schemaHashCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($"SchemaHashCheck does not match, expected: `0x{SchemaHash:X8}`, got: `0x{schemaHashCheck:X8}`");
        
        return new ServerSendRequestDto(
            ServerRoutingDtoSpanSerializer.ReadServerRoutingDto(___span, ref ___offset), 
			PrimitivesSpanSerializer.ReadString(___span, ref ___offset), 
			PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : PrimitivesSpanSerializer.ReadString(___span, ref ___offset), 
			PrimitivesSpanSerializer.ReadByteArray(___span, ref ___offset), 
			PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : PrimitivesSpanSerializer.ReadString(___span, ref ___offset), 
			PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : PrimitivesSpanSerializer.ReadString(___span, ref ___offset), 
			PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : PrimitivesSpanSerializer.ReadString(___span, ref ___offset), 
			PrimitivesSpanSerializer.ReadByteArray(___span, ref ___offset));
    }

    [IsSpanSerializerLength]
    public static int Length(ref int ___offset, ServerSendRequestDto value)
    {
        ___offset += 10;
        ServerRoutingDtoSpanSerializer.Length(ref ___offset, value.Routing);
        PrimitivesSpanSerializer.LengthString(ref ___offset, value.Path);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.QueryString != null);
        if (value.QueryString != null)
            PrimitivesSpanSerializer.LengthString(ref ___offset, value.QueryString);
        PrimitivesSpanSerializer.LengthByteArray(ref ___offset, value.IpAddress);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.CookieData != null);
        if (value.CookieData != null)
            PrimitivesSpanSerializer.LengthString(ref ___offset, value.CookieData);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.StateData != null);
        if (value.StateData != null)
            PrimitivesSpanSerializer.LengthString(ref ___offset, value.StateData);
        PrimitivesSpanSerializer.LengthBoolean(ref ___offset, value.SessionData != null);
        if (value.SessionData != null)
            PrimitivesSpanSerializer.LengthString(ref ___offset, value.SessionData);
        PrimitivesSpanSerializer.LengthByteArray(ref ___offset, value.BinaryData);
        return ___offset;
    }
}