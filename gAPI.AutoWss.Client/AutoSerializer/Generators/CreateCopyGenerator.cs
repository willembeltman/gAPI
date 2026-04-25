using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

#nullable enable

namespace gAPI.AutoSerializer.Generators;

public class CreateCopyGenerator
{
    private readonly INamedTypeSymbol TypeSymbol;
    private readonly CustomObjectMethod[] CustomCreateCopys;
    private readonly HashSet<string> Namespaces = [];

    public string Namespace { get; set; }
    public string Name { get; }
    public string TypeSymbolName { get; }
    public string CreateCopyMethodName { get; }
    public bool IsRecord { get; }
    public PropertyGeneric[] Properties { get; }
    public string FileName { get; }
    public int ItemNumber { get; private set; }
    public List<INamedTypeSymbol> NeededCreateCopys { get; private set; } = new();

    public CreateCopyGenerator(INamedTypeSymbol typeSymbol, CustomObjectMethod[] customCreateCopys)
    {
        TypeSymbol = typeSymbol;
        CustomCreateCopys = customCreateCopys;

        var name = Helper.GetName(typeSymbol);
        Name = $"{name}CreateCopy";
        TypeSymbolName = Helper.GetFullTypeName(typeSymbol, Reg);
        FileName = $"{Name}.g.cs";

        Namespace = TypeSymbol.ContainingNamespace.IsGlobalNamespace
            ? "global"
            : TypeSymbol.ContainingNamespace.ToDisplayString();
        CreateCopyMethodName = "CreateCopy";
        IsRecord = TypeSymbol.IsRecord || TypeSymbol.TypeKind == TypeKind.Struct;
        Properties = Helper.GetProperties(typeSymbol);
    }

    public string Generate()
    {
        var props = TypeSymbol.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.GetMethod != null && p.GetAttributes().Any(a => a.ToString().EndsWith("NotMappedAttribute")) == false)
            .ToArray();

        string propsCode = CreatePropsCode();

        Namespaces.Add("using System.IO;\r\n");
        Namespaces.Add("using gAPI.AttributesSerializers;\r\n");
        Namespaces.Add("using gAPI.Attributes;\r\n");

        var usings = string.Join("", Namespaces.Distinct());
        if (usings.Length > 0) usings = usings + "\r\n";

        return $@"{usings}namespace {Namespace};

public static class {Name}
{{
    [IsCreateCopy]
    public static {TypeSymbolName} {CreateCopyMethodName}(this {TypeSymbolName} value)
    {{{propsCode}
    }}
}}";
    }
    public string CreatePropsCode()
    {
        if (IsRecord)
        {
            var ctor = TypeSymbol.InstanceConstructors
                .Where(c => c.Parameters.Length > 0)
                .OrderByDescending(c => c.Parameters.Length)
                .FirstOrDefault();

            if (ctor != null)
            {
                var args = ctor.Parameters
                    .Select(p =>
                    {
                        var prop = Properties.FirstOrDefault(pr => pr.Property.Name == p.Name);
                        if (prop == null) throw new System.Exception($"Cannot find {p.Name} {ctor} {TypeSymbolName} {p}");
                        return GenerateCopyCode(prop.Property.Type, $"value.{prop.Property.Name}", prop.IsFromGenericParent);
                    });

                return $@"
        return new {TypeSymbolName}({string.Join(", ", args)});";
            }
        }

        // class of record zonder ctor
        return $@"
        var copy = new {TypeSymbolName}();{string.Join("", Properties.Select(prop => $@"
        copy.{prop.Property.Name} = {GenerateCopyCode(prop.Property.Type, $"value.{prop.Property.Name}", prop.IsFromGenericParent)};"))}
        return copy;";
    }

    private string GenerateCopyCode(ITypeSymbol type, string source, bool generic)
    {
        (var underlyingType, var isNullable, var isNullableT) = Helper.GetUnderlayingAndNullable(type);

        var custom = CustomCreateCopys
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.Type, underlyingType));

        if (custom != null)
        {
            Reg(custom.StaticClass.ContainingNamespace.ToDisplayString());

            return isNullable
                ? $"{source} == null ? null : {source}{(isNullableT ? ".Value" : "")}.{custom.Method.Name}()"
                : $"{source}.{custom.Method.Name}()";
        }

        if (underlyingType.TypeKind == TypeKind.Enum)
            return source;

        switch (underlyingType.ToDisplayString())
        {
            case "string":
            case "string?":
            case "int":
            case "long":
            case "bool":
            case "double":
            case "float":
            case "decimal":
            case "System.Guid":
            case "System.DateTime":
            case "System.DateTimeOffset":
                return source;
        }

        // ARRAY
        if (underlyingType is IArrayTypeSymbol array)
        {
            var item = $"item{++ItemNumber}";

            if (array.ElementType.SpecialType == SpecialType.System_Byte)
            {
                return isNullable
                    ? $"{source} == null ? null : {source}.ToArray()"
                    : $"{source}.ToArray()";
            }

            var elementCopy = GenerateCopyCode(array.ElementType, item, generic);

            return isNullable
                ? $@"{source} == null ? null : {source}.Select({item} => {elementCopy}).ToArray()"
                : $"{source}.Select({item} => {elementCopy}).ToArray()";
        }

        if (underlyingType.IsRecord || underlyingType.TypeKind == TypeKind.Struct)
            return source;

        if (underlyingType is INamedTypeSymbol named)
            NeededCreateCopys.Add(named);

        // Complex type
        Reg(underlyingType.ContainingNamespace.ToDisplayString());

        return isNullable
            ? $"{source} == null ? null : {source}{(isNullableT ? ".Value" : "")}.CreateCopy()"
            : $"{source}.CreateCopy()";
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