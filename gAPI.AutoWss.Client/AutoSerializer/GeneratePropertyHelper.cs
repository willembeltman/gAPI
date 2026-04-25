using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoSerializer;

public class GeneratePropertyHelper(
    CustomObject[] CustomSerializers,
    CustomObject[] CustomSpanSerializers,
    CustomObjectMethod[] CustomMultipartFormDataContentSerializers,
    Action<string> Reg,
    List<INamedTypeSymbol> NeededSerializers)
{
    public int ItemNumber { get; private set; }


    public string GenerateBinaryWriterWriteCode(ITypeSymbol type, string propName, string indent, bool fromGeneric)
    {
        (var underlyingType, var isNullable, var isNullableT) = Helper.GetUnderlayingAndNullable(type);

        var customSerializer = CustomSerializers.FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.Type, underlyingType));
        if (customSerializer != null)
        {
            Reg(customSerializer.Writer.StaticClass.ContainingNamespace.ToDisplayString());
            return isNullable
                ? $@"
{indent}___writer.Write({propName} != null);
{indent}if ({propName} != null) 
{indent}   ___writer.Write({propName}{(isNullableT ? ".Value" : "")});"
                : $@"
{indent}___writer.Write({propName});";
        }

        if (underlyingType.TypeKind == TypeKind.Enum)
            return isNullable
                ? $@"
{indent}___writer.Write({propName}.HasValue);
{indent}if ({propName}.HasValue) 
{indent}   ___writer.Write((int){propName}.Value);"
                : $@"
{indent}___writer.Write((int){propName});";

        if (underlyingType.ToDisplayString() == "bool" && fromGeneric)
        {
            return $@"
{indent}___writer.Write({propName});";
        }

        switch (underlyingType.ToDisplayString())
        {
            case "string":
                return $@"
{indent}___writer.Write({propName});";
            case "string?":
                return $@"
{indent}___writer.Write({propName} != null); 
{indent}if ({propName} != null) 
{indent}    ___writer.Write({propName});";
            case "bool":
            case "int":
            case "long":
            case "double":
            case "float":
            case "decimal":
                return isNullable
                    ? $@"
{indent}___writer.Write({propName} != null); 
{indent}if ({propName} != null) 
{indent}    ___writer.Write({propName}.Value);"
                    : $@"
{indent}___writer.Write({propName});";
            case "System.DateTime":
                return isNullable ? $@"
{indent}___writer.Write({propName} != null); 
{indent}if ({propName} != null) 
{indent}    ___writer.Write({propName}.Value.ToBinary());" : $@"
{indent}___writer.Write({propName}.ToBinary());";
            case "System.DateTimeOffset":
                return isNullable ? $@"
{indent}___writer.Write({propName} != null); 
{indent}if ({propName} != null) 
{indent}    ___writer.Write({propName}.Value.ToUnixTimeMilliseconds());" : $@"
{indent}___writer.Write({propName}.ToUnixTimeMilliseconds());";
        }

        // Array !!!!!
        if (underlyingType is IArrayTypeSymbol array)
        {
            if (array.ElementType.Name == "Byte")
            {
                return isNullable
                    ? $@"
{indent}___writer.Write({propName} != null); 
{indent}if ({propName} != null) 
{indent}{{
{indent}    ___writer.Write({propName}.Length);
{indent}    ___writer.Write({propName});
{indent}}}"
                    : $@"
{indent}___writer.Write({propName}.Length);
{indent}___writer.Write({propName});";
            }

            var itemNumber = ++ItemNumber;
            return isNullable
                ? $@"
{indent}___writer.Write({propName} != null); 
{indent}if ({propName} != null) 
{indent}{{
{indent}    ___writer.Write({propName}.Length);
{indent}    foreach(var item{itemNumber} in {propName})
{indent}    {{{GenerateBinaryWriterWriteCode(array.ElementType, $"item{itemNumber}", $"{indent}        ", fromGeneric)}
{indent}    }}
{indent}}}"
                : $@"
{indent}___writer.Write({propName}.Length);
{indent}foreach(var item{itemNumber} in {propName})
{indent}{{{GenerateBinaryWriterWriteCode(array.ElementType, $"item{itemNumber}", $"{indent}    ", fromGeneric)}
{indent}}}";
        }

        if (underlyingType is INamedTypeSymbol named)
            NeededSerializers.Add(named);

        // Niet array !!!!!
        Reg(underlyingType.ContainingNamespace.ToDisplayString());
        NeededSerializers.Add((underlyingType as INamedTypeSymbol)!);
        return isNullable
            ? $@"
{indent}___writer.Write({propName} != null); 
{indent}if ({propName} != null) 
{indent}    {Helper.GetName(underlyingType)}Serializer.Write(___writer, {propName}{(isNullableT ? ".Value" : "")});"
            : $@"
{indent}{Helper.GetName(underlyingType)}Serializer.Write(___writer, {propName});";
    }
    public string GenerateBinaryReaderReadCode(ITypeSymbol type, bool fromGeneric)
    {
        (var underlyingType, var isNullable, var isNullableT) = Helper.GetUnderlayingAndNullable(type);

        var customSerializer = CustomSerializers.FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.Type, underlyingType));
        if (customSerializer != null)
        {
            Reg(customSerializer.Reader.StaticClass.ContainingNamespace.ToDisplayString());
            return isNullable
                ? $"___reader.ReadBoolean() ? ___reader.{customSerializer.Reader.Method.Name}() : null"
                : $"___reader.{customSerializer.Reader.Method.Name}()";
        }

        if (underlyingType.TypeKind == TypeKind.Enum)
            return isNullable
                ? $"___reader.ReadBoolean() ? ({underlyingType.ToDisplayString()})___reader.ReadInt32() : null"
                : $"({underlyingType.ToDisplayString()})___reader.ReadInt32()";

        var start = "";
        if (isNullable)
        {
            start = "___reader.ReadBoolean() == false ? null : ";
        }

        if (underlyingType.ToDisplayString() == "bool" && fromGeneric)
        {
            return "___reader.ReadBoolean()";
        }

        switch (underlyingType.ToDisplayString())
        {
            case "string?": return start + "___reader.ReadString()";
            case "string": return "___reader.ReadString()";
            case "int": return start + "___reader.ReadInt32()";
            case "long": return start + "___reader.ReadInt64()";
            case "bool": return start + "___reader.ReadBoolean()";
            case "double": return start + "___reader.ReadDouble()";
            case "float": return start + "___reader.ReadSingle()";
            case "decimal": return start + "___reader.ReadDecimal()";
            case "System.DateTime": return start + "System.DateTime.FromBinary(___reader.ReadInt64())";
            case "System.DateTimeOffset": return start + "System.DateTimeOffset.FromUnixTimeMilliseconds(___reader.ReadInt64())";
        }

        // Array !!!!!
        if (underlyingType is IArrayTypeSymbol array)
        {
            if (array.ElementType.Name == "Byte")
                return start + $@"___reader.ReadBytes(___reader.ReadInt32())";

            return start + $@"[.. Enumerable.Range(0, ___reader.ReadInt32()).Select(item => {GenerateBinaryReaderReadCode(array.ElementType, fromGeneric)})]";
        }

        var name = Helper.GetName(underlyingType);
        Reg(underlyingType.ContainingNamespace.ToDisplayString());
        NeededSerializers.Add((underlyingType as INamedTypeSymbol)!);
        return start + $"{name}Serializer.Read{name}(___reader)";
    }

    public string GenerateSpanWriteCode(ITypeSymbol type, string propName, string indent, bool fromGeneric)
    {
        (var underlyingType, var isNullable, var isNullableT) = Helper.GetUnderlayingAndNullable(type);

        var customSerializer = CustomSpanSerializers
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.Type, underlyingType));

        if (customSerializer != null)
        {
            Reg(customSerializer.Writer.StaticClass.ContainingNamespace.ToDisplayString());

            return isNullable
                ? $@"
{indent}PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, {propName} != null);
{indent}if ({propName} != null)
{indent}    {customSerializer.Writer.StaticClass.Name}.{customSerializer.Writer.Method.Name}(ref ___span, ref ___offset, {propName}{(isNullableT ? ".Value" : "")});"
                : $@"
{indent}{customSerializer.Writer.StaticClass.Name}.{customSerializer.Writer.Method.Name}(ref ___span, ref ___offset, {propName});";
        }

        if (underlyingType.TypeKind == TypeKind.Enum)
            return isNullable
                ? $@"
{indent}PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, {propName}.HasValue);
{indent}if ({propName}.HasValue)
{indent}    PrimitivesSpanSerializer.WriteInt32(ref ___span, ref ___offset, (int){propName}.Value);"
                : $@"
{indent}PrimitivesSpanSerializer.WriteInt32(ref ___span, ref ___offset, (int){propName});";

        if (underlyingType.ToDisplayString() == "bool" && fromGeneric)
            return $@"
{indent}PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, {propName});";

        switch (underlyingType.ToDisplayString())
        {
            case "int":
                return SpanPrimitiveWrite("Int32", propName, indent, isNullable);
            case "long":
                return SpanPrimitiveWrite("Int64", propName, indent, isNullable);
            case "bool":
                return SpanPrimitiveWrite("Boolean", propName, indent, isNullable);
            case "float":
                return SpanPrimitiveWrite("Single", propName, indent, isNullable);
            case "double":
                return SpanPrimitiveWrite("Double", propName, indent, isNullable);
            case "System.DateTime":
                return SpanPrimitiveWrite("DateTime", propName, indent, isNullable);
            case "System.DateTimeOffset":
                return SpanPrimitiveWrite("DateTimeOffset", propName, indent, isNullable);
            case "string":
                return $@"
{indent}PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, {propName});";
            case "string?":
                return $@"
{indent}PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, {propName} != null);
{indent}if ({propName} != null)
{indent}    PrimitivesSpanSerializer.WriteString(ref ___span, ref ___offset, {propName});";
        }


        // Array !!!!!
        if (underlyingType is IArrayTypeSymbol array)
        {
            if (array.ElementType.Name == "Byte")
            {
                return isNullable
                    ? $@"
{indent}PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, {propName} != null);
{indent}if ({propName} != null) 
{indent}{{
{indent}    PrimitivesSpanSerializer.WriteByteArray(ref ___span, ref ___offset, {propName});
{indent}}}"
                    : $@"
{indent}PrimitivesSpanSerializer.WriteByteArray(ref ___span, ref ___offset, {propName});";
            }

            var itemNumber = ++ItemNumber;
            return isNullable
                ? $@"
{indent}PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, {propName} != null);
{indent}if ({propName} != null) 
{indent}{{
{indent}    PrimitivesSpanSerializer.WriteInt32(ref ___span, ref ___offset, {propName}.Length);
{indent}    foreach(var item{itemNumber} in {propName})
{indent}    {{{GenerateSpanWriteCode(array.ElementType, $"item{itemNumber}", $"{indent}        ", fromGeneric)}
{indent}    }}
{indent}}}"
                : $@"
{indent}PrimitivesSpanSerializer.WriteInt32(ref ___span, ref ___offset, {propName}.Length);
{indent}foreach(var item{itemNumber} in {propName})
{indent}{{{GenerateSpanWriteCode(array.ElementType, $"item{itemNumber}", $"{indent}    ", fromGeneric)}
{indent}}}";
        }

        if (underlyingType is INamedTypeSymbol named)
            NeededSerializers.Add(named);

        Reg(underlyingType.ContainingNamespace.ToDisplayString());
        NeededSerializers.Add((underlyingType as INamedTypeSymbol)!);
        return isNullable
            ? $@"
{indent}PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, {propName} != null);
{indent}if ({propName} != null)
{indent}    {Helper.GetName(underlyingType)}SpanSerializer.Write(ref ___span, ref ___offset, {propName}{(isNullableT ? ".Value" : "")});"
            : $@"
{indent}{Helper.GetName(underlyingType)}SpanSerializer.Write(ref ___span, ref ___offset, {propName});";
    }
    private string SpanPrimitiveWrite(string method, string propName, string indent, bool nullable)
    {
        return nullable
            ? $@"
{indent}PrimitivesSpanSerializer.WriteBoolean(ref ___span, ref ___offset, {propName} != null);
{indent}if ({propName} != null)
{indent}    PrimitivesSpanSerializer.Write{method}(ref ___span, ref ___offset, {propName}.Value);"
            : $@"
{indent}PrimitivesSpanSerializer.Write{method}(ref ___span, ref ___offset, {propName});";
    }

    public string GenerateSpanReadCode(ITypeSymbol type, bool fromGeneric, ref string functions)
    {
        (var underlyingType, var isNullable, var isNullableT) = Helper.GetUnderlayingAndNullable(type);

        var start = isNullable ? "PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : " : "";

        var customSerializer = CustomSpanSerializers
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.Type, underlyingType));

        if (customSerializer != null)
        {
            Reg(customSerializer.Reader.StaticClass.ContainingNamespace.ToDisplayString());
            return start + $"{customSerializer.Reader.StaticClass.Name}.{customSerializer.Reader.Method.Name}(___span, ref ___offset)";
        }

        if (underlyingType.TypeKind == TypeKind.Enum)
            return start + $"({underlyingType})PrimitivesSpanSerializer.ReadInt32(___span, ref ___offset)";

        if (underlyingType.ToDisplayString() == "bool" && fromGeneric)
            return $"PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset)";

        switch (underlyingType.ToDisplayString())
        {
            case "int": return SpanNullableRead("Int32", isNullable);
            case "long": return SpanNullableRead("Int64", isNullable);
            case "bool": return SpanNullableRead("Boolean", isNullable);
            case "float": return SpanNullableRead("Single", isNullable);
            case "double": return SpanNullableRead("Double", isNullable);
            case "System.DateTime": return SpanNullableRead("DateTime", isNullable);
            case "System.DateTimeOffset": return SpanNullableRead("DateTimeOffset", isNullable);
            case "string": return $"PrimitivesSpanSerializer.ReadString(___span, ref ___offset)";
            case "string?": return start + $"PrimitivesSpanSerializer.ReadString(___span, ref ___offset)";
        }

        // Array !!!!!
        if (underlyingType is IArrayTypeSymbol array)
        {
            //var newOffset = $"____offset";
            if (array.ElementType.Name == "Byte")
                return start + $@"PrimitivesSpanSerializer.ReadByteArray(___span, ref ___offset)";

            Reg(array.ElementType.ContainingNamespace.ToDisplayString());
            functions += $@"

    static {array.ElementType.Name}{(array.ElementType.NullableAnnotation.HasFlag(NullableAnnotation.Annotated) ? "?" : "")}[] BuildList(ReadOnlySpan<byte> ___span, ref int ___offset, int count)
    {{
        var list = new List<{array.ElementType.Name}{(array.ElementType.NullableAnnotation.HasFlag(NullableAnnotation.Annotated) ? "?" : "")}>(count);
        for (int i = 0; i < count; i++)
        {{
            var item = {GenerateSpanReadCode(array.ElementType, fromGeneric, ref functions)};
            list.Add(item);
        }}
        return [.. list];
    }}";

            return start + $@"BuildList(___span, ref ___offset, PrimitivesSpanSerializer.ReadInt32(___span, ref ___offset))";
        }

        var name = Helper.GetName(underlyingType);
        Reg(underlyingType.ContainingNamespace.ToDisplayString());
        NeededSerializers.Add((underlyingType as INamedTypeSymbol)!);
        return start + $"{name}SpanSerializer.Read{name}(___span, ref ___offset)";
    }
    private string SpanNullableRead(string method, bool nullable)
    {
        return nullable
            ? $"PrimitivesSpanSerializer.ReadBoolean(___span, ref ___offset) == false ? null : PrimitivesSpanSerializer.Read{method}(___span, ref ___offset)"
            : $"PrimitivesSpanSerializer.Read{method}(___span, ref ___offset)";
    }

    public string GenerateLengthCode(ITypeSymbol type, string propName, string indent, bool fromGeneric)
    {
        (var underlyingType, var isNullable, var isNullableT) = Helper.GetUnderlayingAndNullable(type);

        var customSerializer = CustomSpanSerializers
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.Type, underlyingType));

        if (customSerializer != null)
        {
            Reg(customSerializer.Length.StaticClass.ContainingNamespace.ToDisplayString());

            return isNullable
                ? $@"
{indent}PrimitivesSpanSerializer.LengthBoolean(ref ___offset, {propName} != null);
{indent}if ({propName} != null)
{indent}    {customSerializer.Length.StaticClass.Name}.{customSerializer.Length.Method.Name}(ref ___offset, {propName}{(isNullableT ? ".Value" : "")});"
                : $@"
{indent}{customSerializer.Length.StaticClass.Name}.{customSerializer.Length.Method.Name}(ref ___offset, {propName});";
        }

        if (underlyingType.TypeKind == TypeKind.Enum)
            return isNullable
                ? $@"
{indent}PrimitivesSpanSerializer.LengthBoolean(ref ___offset, {propName}.HasValue);
{indent}if ({propName}.HasValue)
{indent}    PrimitivesSpanSerializer.LengthInt32(ref ___offset, (int){propName}.Value);"
                : $@"
{indent}PrimitivesSpanSerializer.LengthInt32(ref ___offset, (int){propName});";

        if (underlyingType.ToDisplayString() == "bool" && fromGeneric)
            return $@"
{indent}PrimitivesSpanSerializer.LengthBoolean(ref ___offset, {propName});";

        switch (underlyingType.ToDisplayString())
        {
            case "int":
                return PrimitiveLength("Int32", propName, indent, isNullable);
            case "long":
                return PrimitiveLength("Int64", propName, indent, isNullable);
            case "bool":
                return PrimitiveLength("Boolean", propName, indent, isNullable);
            case "float":
                return PrimitiveLength("Single", propName, indent, isNullable);
            case "double":
                return PrimitiveLength("Double", propName, indent, isNullable);
            case "System.DateTime":
                return PrimitiveLength("DateTime", propName, indent, isNullable);
            case "System.DateTimeOffset":
                return PrimitiveLength("DateTimeOffset", propName, indent, isNullable);
            case "string":
                return $@"
{indent}PrimitivesSpanSerializer.LengthString(ref ___offset, {propName});";
            case "string?":
                return $@"
{indent}PrimitivesSpanSerializer.LengthBoolean(ref ___offset, {propName} != null);
{indent}if ({propName} != null)
{indent}    PrimitivesSpanSerializer.LengthString(ref ___offset, {propName});";
        }


        // Array !!!!!
        if (underlyingType is IArrayTypeSymbol array)
        {
            if (array.ElementType.Name == "Byte")
            {
                return isNullable
                    ? $@"
{indent}PrimitivesSpanSerializer.LengthBoolean(ref ___offset, {propName} != null);
{indent}if ({propName} != null) 
{indent}{{
{indent}    PrimitivesSpanSerializer.LengthByteArray(ref ___offset, {propName});
{indent}}}"
                    : $@"
{indent}PrimitivesSpanSerializer.LengthByteArray(ref ___offset, {propName});";
            }

            var itemNumber = ++ItemNumber;
            return isNullable
                ? $@"
{indent}PrimitivesSpanSerializer.LengthBoolean(ref ___offset, {propName} != null);
{indent}if ({propName} != null) 
{indent}{{
{indent}    PrimitivesSpanSerializer.LengthInt32(ref ___offset, {propName}.Length);
{indent}    foreach(var item{itemNumber} in {propName})
{indent}    {{{GenerateLengthCode(array.ElementType, $"item{itemNumber}", $"{indent}        ", fromGeneric)}
{indent}    }}
{indent}}}"
                : $@"
{indent}PrimitivesSpanSerializer.LengthInt32(ref ___offset, {propName}.Length);
{indent}foreach(var item{itemNumber} in {propName})
{indent}{{{GenerateLengthCode(array.ElementType, $"item{itemNumber}", $"{indent}    ", fromGeneric)}
{indent}}}";
        }

        if (underlyingType is INamedTypeSymbol named)
            NeededSerializers.Add(named);

        Reg(underlyingType.ContainingNamespace.ToDisplayString());
        NeededSerializers.Add((underlyingType as INamedTypeSymbol)!);
        return isNullable
            ? $@"
{indent}PrimitivesSpanSerializer.LengthBoolean(ref ___offset, {propName} != null);
{indent}if ({propName} != null)
{indent}    {Helper.GetName(underlyingType)}SpanSerializer.Length(ref ___offset, {propName}{(isNullableT ? ".Value" : "")});"
            : $@"
{indent}{Helper.GetName(underlyingType)}SpanSerializer.Length(ref ___offset, {propName});";
    }
    private string PrimitiveLength(string method, string propName, string indent, bool nullable)
    {
        return nullable
            ? $@"
{indent}PrimitivesSpanSerializer.LengthBoolean(ref ___offset, {propName} != null);
{indent}if ({propName} != null)
{indent}    PrimitivesSpanSerializer.Length{method}(ref ___offset, {propName}.Value);"
            : $@"
{indent}PrimitivesSpanSerializer.Length{method}(ref ___offset, {propName});";
    }

    public string GenerateMultipartFormDataContentWriteCode(ITypeSymbol type, string propName, string fieldName, string indent, bool fromGeneric, bool fromApiClient = false)
    {
        (var underlyingType, var isNullable, var isNullableT) = Helper.GetUnderlayingAndNullable(type);

        var customSerializer = CustomMultipartFormDataContentSerializers
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.Type, underlyingType));

        var name = fromApiClient ?
            $@"""{fieldName}""" :
            $@"$""{{___name}}.{fieldName}""";

        // CUSTOM SERIALIZER
        if (customSerializer != null)
        {
            Reg(customSerializer.StaticClass.ContainingNamespace.ToDisplayString());

            return isNullable
                ? $@"
{indent}if ({propName} != null)
{indent}    {customSerializer.StaticClass.Name}.{customSerializer.Method.Name}(___content, {name}, {propName}{(isNullableT ? ".Value" : "")});"
                : $@"
{indent}{customSerializer.StaticClass.Name}.{customSerializer.Method.Name}(___content, {name}, {propName});";
        }

        // ENUM
        if (underlyingType.TypeKind == TypeKind.Enum)
        {
            return isNullable
                ? $@"
{indent}if ({propName}.HasValue)
{indent}    ___content.Add(new StringContent(((int){propName}.Value).ToString()), {name});"
                : $@"
{indent}___content.Add(new StringContent(((int){propName}).ToString()), {name});";
        }

        switch (underlyingType.ToDisplayString())
        {
            case "int":
            case "long":
            case "float":
            case "double":
            case "bool":
                return isNullable
                    ? $@"
{indent}if ({propName}.HasValue)
{indent}    ___content.Add(new StringContent({propName}.Value.ToString()), {name});"
                    : $@"
{indent}___content.Add(new StringContent({propName}.ToString()), {name});";

            case "System.DateTime":
            case "System.DateTimeOffset":
                return isNullable
                    ? $@"
{indent}if ({propName}.HasValue)
{indent}    ___content.Add(new StringContent({propName}.Value.ToString(""O"")), {name});"
                    : $@"
{indent}___content.Add(new StringContent({propName}.ToString(""O"")), {name});";

            case "string":
                return $@"
{indent}___content.Add(new StringContent({propName}), {name});";

            case "string?":
                return $@"
{indent}if ({propName} != null)
{indent}    ___content.Add(new StringContent({propName}), {name});";
        }

        // Array !!!!! 
        if (underlyingType is IArrayTypeSymbol array)
        {
            // BYTE ARRAY (file upload)
            if (array.ElementType.Name == "Byte")
            {
                return isNullable
                    ? $@"
{indent}if ({propName} != null)
{indent}    ___content.Add(new ByteArrayContent({propName}), {name}, ""file"");"
                    : $@"
{indent}___content.Add(new ByteArrayContent({propName}), {name}, ""file"");";
            }

            var itemNumber = ++ItemNumber;

            return isNullable
                ? $@"
{indent}if ({propName} != null)
{indent}{{
{indent}    foreach (var item{itemNumber} in {propName}{(isNullableT ? ".Value" : "")})
{indent}    {{
{GenerateMultipartFormDataContentWriteCode(array.ElementType, $"item{itemNumber}", fieldName, indent + "        ", fromGeneric)}
{indent}    }}
{indent}}}"
                : $@"
{indent}foreach (var item{itemNumber} in {propName})
{indent}{{
{GenerateMultipartFormDataContentWriteCode(array.ElementType, $"item{itemNumber}", fieldName, indent + "    ", fromGeneric)}
{indent}}}";
        }

        // NESTED OBJECT
        if (underlyingType is INamedTypeSymbol named)
            NeededSerializers.Add(named);

        Reg(underlyingType.ContainingNamespace.ToDisplayString());

        return isNullable
            ? $@"
{indent}if ({propName} != null)
{indent}    {Helper.GetName(underlyingType)}MultipartFormDataContentSerializer.Write(___content, {name}, {propName}{(isNullableT ? ".Value" : "")});"
            : $@"
{indent}{Helper.GetName(underlyingType)}MultipartFormDataContentSerializer.Write(___content, {name}, {propName});";
    }
}
