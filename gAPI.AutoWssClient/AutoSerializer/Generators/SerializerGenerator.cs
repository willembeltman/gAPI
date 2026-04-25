using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

#nullable enable

namespace gAPI.AutoSerializer.Generators;

public class SerializerGenerator
{
    private readonly INamedTypeSymbol TypeSymbol;
    private readonly HashSet<string> Namespaces = [];

    public string Namespace { get; set; }
    public string Name { get; }
    public string TypeSymbolName { get; }
    public string ReadMethodName { get; }
    public string WriteMethodName { get; }
    public bool IsRecord { get; }
    public PropertyGeneric[] Properties { get; }
    public uint Typehash { get; }
    public string FileName { get; }
    public List<INamedTypeSymbol> NeededSerializers { get; private set; } = new();
    public uint SchemaHash { get; }
    public GeneratePropertyHelper PropertyHelper { get; }

    public SerializerGenerator(INamedTypeSymbol typeSymbol, CustomObject[] customSerializers)
    {
        TypeSymbol = typeSymbol;

        var name = Helper.GetName(typeSymbol);
        Name = $"{name}Serializer";
        TypeSymbolName = Helper.GetFullTypeName(typeSymbol, Reg);
        FileName = $"{Name}.g.cs";

        Namespace = TypeSymbol.ContainingNamespace.IsGlobalNamespace
            ? "global"
            : TypeSymbol.ContainingNamespace.ToDisplayString();
        ReadMethodName = $"Read{name}";
        WriteMethodName = "Write";
        IsRecord = TypeSymbol.IsRecord || TypeSymbol.TypeKind == TypeKind.Struct;
        Properties = Helper.GetProperties(typeSymbol);

        // --- TypeHash: hash van fully qualified type name (TypeId) ---
        Typehash = Helper.ComputeFNV1a32(TypeSymbolName);

        // --- VersionHash: hash van schema (properties + nested types) ---
        SchemaHash = Helper.ComputeFNV1a32(string.Join("|", Properties.Select(a => $"{a.Property.Type.Name} {a.Property.Name}")));

        PropertyHelper = new GeneratePropertyHelper(customSerializers, [], [], Reg, NeededSerializers);
    }

    public string Generate()
    {
        var props = TypeSymbol.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.GetMethod != null && p.GetAttributes().Any(a => a.ToString().EndsWith("NotMappedAttribute")) == false)
            .ToArray();

        string writeProps = CreateWriteProps();
        string readProps = CreateReadProps();

        Namespaces.Add("using System.IO;\r\n");
        Namespaces.Add("using gAPI.AttributesSerializers;\r\n");
        Namespaces.Add("using gAPI.Attributes;\r\n");

        var usings = string.Join("", Namespaces.Distinct());
        if (usings.Length > 0) usings = usings + "\r\n";

        return $@"{usings}#nullable enable
namespace {Namespace};

public static class {Name}
{{
    public const ushort Magic = (ushort)0x4741;
    public const uint TypeId = 0x{Typehash:X8};
    public const uint SchemaHash = 0x{SchemaHash:X8};

    [IsSerializerWrite]
    public static void Write(this BinaryWriter ___writer, {TypeSymbolName} value)
    {{
        ___writer.Write(Magic); // Magic string `GA` => it's a gAPI stream
        ___writer.Write(TypeId); // Type identifier
        ___writer.Write(SchemaHash); // Schema identifier
        {writeProps}
    }}

    [IsSerializerRead]
    public static {TypeSymbolName} {ReadMethodName}(this BinaryReader ___reader)
    {{
        var magicCheck = ___reader.ReadUInt16();// Magic string `GA` => it's a gAPI stream
        if (magicCheck != Magic) throw new InvalidDataException($""magic does not match, expected: `0x{{Magic:X4}}`, got: `0x{{magicCheck:X4}}`"");
        var typeIdCheck = ___reader.ReadUInt32(); // Type identifier
        if (typeIdCheck != TypeId) throw new InvalidDataException($""TypeIdCheck does not match, expected: `0x{{TypeId:X8}}`, got: `0x{{typeIdCheck:X8}}`"");
        var schemaHashCheck = ___reader.ReadUInt32(); // Schema identifier
        if (schemaHashCheck != SchemaHash) throw new InvalidDataException($""SchemaHashCheck does not match, expected: `0x{{SchemaHash:X8}}`, got: `0x{{schemaHashCheck:X8}}`"");
        {readProps}
    }}
}}";
    }

    public string CreateWriteProps()
    {
        return string.Join(
            "",
            Properties.Select(prop => PropertyHelper.GenerateBinaryWriterWriteCode(prop.Property.Type, $"value.{prop.Property.Name}", "        ", prop.IsFromGenericParent)));
    }
    public string CreateReadProps()
    {
        string readProps;

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
                        return PropertyHelper.GenerateBinaryReaderReadCode(prop.Property.Type, prop.IsFromGenericParent);
                    })
                    .ToArray();

                readProps = $@"
        return new {TypeSymbolName}({string.Join(", ", args)});";
            }
            else
            {
                readProps = $@"            
        var value = new {TypeSymbolName}();{string.Join("", Properties.Select(prop => $@"
        value.{prop.Property.Name} = {PropertyHelper.GenerateBinaryReaderReadCode(prop.Property.Type, prop.IsFromGenericParent)};"))}
        return value;";
            }
        }
        else
        {
            readProps = $@"
        var value = new {TypeSymbolName}();{string.Join("", Properties.Select(prop => $@"
        value.{prop.Property.Name} = {PropertyHelper.GenerateBinaryReaderReadCode(prop.Property.Type, prop.IsFromGenericParent)};"))}
        return value;";
        }

        return readProps;
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