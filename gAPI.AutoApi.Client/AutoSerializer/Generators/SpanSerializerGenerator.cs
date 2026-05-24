using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace gAPI.AutoSerializer.Generators;

public class SpanSerializerGenerator
{
    private readonly INamedTypeSymbol TypeSymbol;
    private readonly HashSet<string> Namespaces = [];

    public string Namespace { get; set; }
    public string Name { get; }
    public string TypeSymbolName { get; }
    public string ReadMethodName { get; }
    public string WriteMethodName { get; }
    public string LengthMethodName { get; }
    public bool IsRecord { get; }
    public PropertyGeneric[] Properties { get; }
    public uint Typehash { get; }
    public uint SchemaHash { get; }
    public GeneratePropertyHelper PropertyHelper { get; }
    public string FileName { get; }
    public List<INamedTypeSymbol> NeededSerializers { get; private set; } = new();

    public SpanSerializerGenerator(INamedTypeSymbol typeSymbol, CustomObject[] customSpanSerializers)
    {
        TypeSymbol = typeSymbol;

        var name = Helper.GetName(typeSymbol);
        Name = $"{name}SpanSerializer";
        TypeSymbolName = Helper.GetFullTypeName(typeSymbol, Reg);
        FileName = $"{Name}.g.cs";

        Namespace = TypeSymbol.ContainingNamespace.IsGlobalNamespace
            ? "global"
            : TypeSymbol.ContainingNamespace.ToDisplayString();
        ReadMethodName = $"Read{name}";
        WriteMethodName = "Write";
        LengthMethodName = "Length";
        IsRecord = TypeSymbol.IsRecord || TypeSymbol.TypeKind == TypeKind.Struct;
        Properties = Helper.GetProperties(typeSymbol);

        // --- TypeHash: hash van fully qualified type name (TypeId) ---
        Typehash = Helper.ComputeFNV1a32(TypeSymbolName);

        // --- VersionHash: hash van schema (properties + nested types) ---
        SchemaHash = Helper.ComputeFNV1a32(string.Join("|", Properties.Select(a => $"{a.Property.Type.Name} {a.Property.Name}")));

        PropertyHelper = new GeneratePropertyHelper([], customSpanSerializers, [], Reg, NeededSerializers);
    }

    public string Generate()
    {
        var props = TypeSymbol.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.GetMethod != null && p.GetAttributes().Any(a => a.ToString().EndsWith("NotMappedAttribute")) == false)
            .ToArray();

        string writeProps = CreateWriteProps();
        (string readProps, string functions) = CreateReadProps();
        string lengthProps = CreateLengthProps();

        Namespaces.Add("using System;\r\n");
        Namespaces.Add("using System.Buffers.Binary;\r\n");
        Namespaces.Add("using System.Text;\r\n");
        Namespaces.Add("using gAPI.Core.AttributesSerializers;\r\n");
        Namespaces.Add("using gAPI.Core.Attributes;\r\n");
        Namespaces.Add("using gAPI.Core.Serializers;\r\n");

        var usings = string.Join("", Namespaces.Distinct().OrderBy(a => a));
        if (usings.Length > 0) usings = usings + "\r\n";

        return $@"{usings}#nullable enable
namespace {Namespace};

public static class {Name}
{{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0x{Typehash:X8};
    public const uint SchemaHash = 0x{SchemaHash:X8};

    [IsSpanSerializerWrite]
    public static void {WriteMethodName}(this ref Span<byte> ___span, ref int ___offset, {TypeSymbolName} value)
    {{
        PrimitivesSpanSerializer.WriteUShort(ref ___span, ref ___offset, Magic); // Magic string `GA` => it's a gAPI stream
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, TypeId); // Type identifier
        PrimitivesSpanSerializer.WriteUInt(ref ___span, ref ___offset, SchemaHash); // Schema identifier
        {writeProps}
    }}

    [IsSpanSerializerRead]
    public static {TypeSymbolName} {ReadMethodName}(this ReadOnlySpan<byte> ___span, ref int ___offset)
    {{
        var magicCheck = PrimitivesSpanSerializer.ReadUShort(___span, ref ___offset);// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($""magic does not match, expected: `0x{{Magic:X4}}`, got: `0x{{magicCheck:X4}}`"");
        var typeIdCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($""TypeIdCheck does not match, expected: `0x{{TypeId:X8}}`, got: `0x{{typeIdCheck:X8}}`"");
        var schemaHashCheck = PrimitivesSpanSerializer.ReadUInt(___span, ref ___offset); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($""SchemaHashCheck does not match, expected: `0x{{SchemaHash:X8}}`, got: `0x{{schemaHashCheck:X8}}`"");
        {readProps}
    }}

    [IsSpanSerializerLength]
    public static int {LengthMethodName}(ref int ___offset, {TypeSymbolName} value)
    {{
        ___offset += 10;{lengthProps}
        return ___offset;
    }}{functions}
}}";
    }

    private string CreateLengthProps()
    {
        return string.Join(
            "",
            Properties.Select(prop => PropertyHelper.GenerateLengthCode(prop.Property.Type, $"value.{prop.Property.Name}", "        ", prop.IsFromGenericParent)));
    }
    public string CreateWriteProps()
    {
        return string.Join(
            "",
            Properties.Select(prop => PropertyHelper.GenerateSpanWriteCode(prop.Property.Type, $"value.{prop.Property.Name}", "        ", prop.IsFromGenericParent)));
    }
    public (string readprops, string functions) CreateReadProps()
    {
        var readProps = string.Empty; 
        var functions = string.Empty; 
        var functionNames = new HashSet<string>();

        if (IsRecord)
        {
            // Vul record constructor parameters
            var ctor = TypeSymbol.InstanceConstructors
                .Where(c => c.Parameters.Length > 0)
                .OrderByDescending(c => c.Parameters.Length)
                .FirstOrDefault();

            if (ctor != null)
            {
                var args = ctor.Parameters
                    .Select(p =>
                    {
                        var prop = Properties.FirstOrDefault(pr => pr.Property.Name == p.Name) ?? Properties[0];
                        return PropertyHelper.GenerateSpanReadCode(prop.Property.Type, prop.IsFromGenericParent, ref functions, functionNames);
                    })
                    .ToArray();

                readProps = $@"
        return new {TypeSymbolName}({string.Join(", ", args)});";
            }
            else
            {
                readProps = $@"            
        var value = new {TypeSymbolName}();{string.Join("", Properties.Select(prop => $@"
        value.{prop.Property.Name} = {PropertyHelper.GenerateSpanReadCode(prop.Property.Type, prop.IsFromGenericParent, ref functions, functionNames)};"))}
        return value;";
            }
        }
        else
        {
            readProps = $@"
        var value = new {TypeSymbolName}();{string.Join("", Properties.Select(prop => $@"
        value.{prop.Property.Name} = {PropertyHelper.GenerateSpanReadCode(prop.Property.Type, prop.IsFromGenericParent, ref functions, functionNames)};"))}
        return value;";
        }

        return (readProps, functions);
    }

    private void Reg(ITypeSymbol underlyingType)
    {
        if (!string.IsNullOrEmpty(underlyingType.ContainingNamespace?.ToString()))
        {
            var nsCode = $"using {underlyingType.ContainingNamespace};\r\n";
            if (!Namespaces.Contains(nsCode))
                Namespaces.Add(nsCode);
        }
    }
    private void Reg(string ns)
    {
        if (!string.IsNullOrEmpty(ns))
        {
            var nsCode = $"using {ns};\r\n";
            if (!Namespaces.Contains(nsCode))
                Namespaces.Add(nsCode);
        }
    }
}