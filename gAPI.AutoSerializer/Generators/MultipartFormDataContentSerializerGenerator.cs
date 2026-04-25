using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace gAPI.AutoSerializer.Generators;

public class MultipartFormDataContentSerializerGenerator
{
    private readonly INamedTypeSymbol TypeSymbol;
    private readonly HashSet<string> Namespaces = [];

    public string Namespace { get; set; }
    public string Name { get; }
    public string TypeSymbolName { get; }
    public string WriteMethodName { get; }
    public bool IsRecord { get; }
    public PropertyGeneric[] Properties { get; }
    public GeneratePropertyHelper PropertyHelper { get; }
    public string FileName { get; }
    public List<INamedTypeSymbol> NeededSerializers { get; private set; } = new();

    public MultipartFormDataContentSerializerGenerator(INamedTypeSymbol typeSymbol, CustomObjectMethod[] customMultipartFormDataContentSerializers)
    {
        TypeSymbol = typeSymbol;

        var name = Helper.GetName(typeSymbol);
        Name = $"{name}MultipartFormDataContentSerializer";
        TypeSymbolName = Helper.GetFullTypeName(typeSymbol, Reg);
        FileName = $"{Name}.g.cs";

        Namespace = TypeSymbol.ContainingNamespace.IsGlobalNamespace
            ? "global"
            : TypeSymbol.ContainingNamespace.ToDisplayString();
        WriteMethodName = "Write";
        IsRecord = TypeSymbol.IsRecord || TypeSymbol.TypeKind == TypeKind.Struct;
        Properties = Helper.GetProperties(typeSymbol);

        PropertyHelper = new GeneratePropertyHelper([], [], customMultipartFormDataContentSerializers, Reg, NeededSerializers);
    }

    public string Generate()
    {
        var props = TypeSymbol.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.GetMethod != null && p.GetAttributes().Any(a => a.ToString().EndsWith("NotMappedAttribute")) == false)
            .ToArray();

        string writeProps = CreateWriteProps();

        Namespaces.Add("using System;\r\n");
        Namespaces.Add("using System.Buffers.Binary;\r\n");
        Namespaces.Add("using System.Text;\r\n");
        Namespaces.Add("using gAPI.AttributesSerializers;\r\n");
        Namespaces.Add("using gAPI.Attributes;\r\n");
        Namespaces.Add("using gAPI.Serializers;\r\n");

        var usings = string.Join("", Namespaces.Distinct().OrderBy(a => a));
        if (usings.Length > 0) usings = usings + "\r\n";

        return $@"{usings}#nullable enable
namespace {Namespace};

public static class {Name}
{{

    [IsMultipartFormDataContentSerializer]
    public static void {WriteMethodName}(this MultipartFormDataContent ___content, string ___name, {TypeSymbolName} value)
    {{{writeProps}
    }}
}}";
    }

    public string CreateWriteProps()
    {
        return string.Join(
            "",
            Properties.Select(prop =>
                PropertyHelper.GenerateMultipartFormDataContentWriteCode(
                    prop.Property.Type,
                    $@"value.{prop.Property.Name}",
                    prop.Property.Name,
                    "        ",
                    prop.IsFromGenericParent
                )));
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