using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

#nullable enable

namespace gAPI.AutoSerializer.Generators;

public class ComparerGenerator
{
    private readonly INamedTypeSymbol TypeSymbol;
    private readonly CustomObjectMethod[] CustomComparers;
    private readonly HashSet<string> Namespaces = [];

    public string Namespace { get; set; }
    public string Name { get; }
    public string TypeSymbolName { get; }
    public string CompareMethodName { get; }
    public bool IsRecord { get; }
    public PropertyGeneric[] Properties { get; }
    public string FileName { get; }
    public int ItemNumber { get; private set; }
    public List<INamedTypeSymbol> NeededComparers { get; private set; } = new();

    public ComparerGenerator(INamedTypeSymbol typeSymbol, CustomObjectMethod[] customComparers)
    {
        TypeSymbol = typeSymbol;
        CustomComparers = customComparers;

        var name = Helper.GetName(typeSymbol);
        Name = $"{name}Comparer";
        TypeSymbolName = Helper.GetFullTypeName(typeSymbol, Reg);
        FileName = $"{Name}.g.cs";

        Namespace = TypeSymbol.ContainingNamespace.IsGlobalNamespace
            ? "global"
            : TypeSymbol.ContainingNamespace.ToDisplayString();
        CompareMethodName = "IsDifferent";
        IsRecord = TypeSymbol.IsRecord || TypeSymbol.TypeKind == TypeKind.Struct;
        Properties = Helper.GetProperties(typeSymbol);
    }

    public string Generate()
    {
        var props = TypeSymbol.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.GetMethod != null && p.GetAttributes().Any(a => a.ToString().EndsWith("NotMappedAttribute")) == false)
            .ToArray();

        string writeProps = CreateProps();

        Namespaces.Add("using System.IO;\r\n");
        Namespaces.Add("using gAPI.AttributesSerializers;\r\n");
        Namespaces.Add("using gAPI.Attributes;\r\n");

        var usings = string.Join("", Namespaces.Distinct());
        if (usings.Length > 0) usings = usings + "\r\n";

        return $@"{usings}namespace {Namespace};

public static class {Name}
{{
    [IsComparer]
    public static bool {CompareMethodName}(this {TypeSymbolName} value, {TypeSymbolName} otherValue)
    {{{writeProps}
        return false;
    }}
}}";
    }

    public string CreateProps()
    {
        return string.Join(
            "",
            Properties.Select(prop => GenerateComparerProp(prop.Property.Type, $"value.{prop.Property.Name}", $"otherValue.{prop.Property.Name}", "        ", prop.IsFromGenericParent)));
    }

    private string GenerateComparerProp(ITypeSymbol type, string propName, string otherPropName, string indent, bool generic)
    {
        (var underlyingType, var isNullable, var isNullableT) = Helper.GetUnderlayingAndNullable(type);

        var customComparer = CustomComparers.FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.Type, underlyingType));
        if (customComparer != null)
        {
            Reg(customComparer.StaticClass.ContainingNamespace.ToDisplayString());
            return isNullable
                ? $@"
{indent}if ({propName} is null)
{indent}{{
{indent}    if ({otherPropName} is not null) return true;
{indent}}}
{indent}else
{indent}{{
{indent}    if ({otherPropName} is null) return true;
{indent}    if ({propName}.{customComparer.Method.Name}({otherPropName})) return true;
{indent}}}"
                : $@"
{indent}if ({propName}.{customComparer.Method.Name}({otherPropName})) return true;";
            //            return isNullable
            //                    ? $@"
            //{indent}if ({propName}?.{customComparer.Method.Name}({otherPropName}) ?? {otherPropName} != null) return true;"
            //                    : $@"
            //{indent}if ({propName}.{customComparer.Method.Name}({otherPropName})) return true;";
        }

        if (underlyingType.TypeKind == TypeKind.Enum)
            return $@"
{indent}if ({propName} != {otherPropName}) return true;";

        if (underlyingType.ToDisplayString() == "bool" && generic)
            return $@"

{indent}if ({propName} != {otherPropName}) return true;";

        switch (underlyingType.ToDisplayString())
        {
            case "string":
            case "string?":
            case "bool":
            case "int":
            case "long":
            case "double":
            case "float":
            case "decimal":
            case "System.Guid":
            case "System.DateTime":
            case "System.DateTimeOffset":
                return $@"
{indent}if ({propName} != {otherPropName}) return true;";
        }

        // Array !!!!!
        if (underlyingType is IArrayTypeSymbol array)
        {
            if (array.ElementType.Name == "Byte")
            {
                return isNullable
                    ? $@"
{indent}if (!({propName}?.AsSpan().SequenceEqual({otherPropName}) ?? {otherPropName} is null)) return true;"
                    : $@"
{indent}if ({propName}.AsSpan().SequenceEqual({otherPropName}) == false) return true;";
            }

            var itemNumber = ++ItemNumber;
            return isNullable
                ? $@"
{indent}if ({propName} is null)
{indent}{{
{indent}    if ({otherPropName} is not null) return true;
{indent}}}
{indent}else
{indent}{{
{indent}    if ({otherPropName} is null) return true;
{indent}    if ({propName}.Length != {otherPropName}.Length) return true;
{indent}    for (int i{itemNumber} = 0; i{itemNumber} < {propName}.Length; i{itemNumber}++)
{indent}    {{
{indent}        var item{itemNumber} = {propName}[i{itemNumber}];
{indent}        var otherItem{itemNumber} = {otherPropName}[i{itemNumber}];{GenerateComparerProp(array.ElementType, $"item{itemNumber}", $"otherItem{itemNumber}", $"{indent}        ", generic)}
{indent}    }}
{indent}}}"
                : $@"
{indent}if ({propName}.Length != {otherPropName}.Length) return true;
{indent}for (int i{itemNumber} = 0; i{itemNumber} < {propName}.Length; i{itemNumber}++)
{indent}{{
{indent}    var item{itemNumber} = {propName}[i{itemNumber}];
{indent}    var otherItem{itemNumber} = {otherPropName}[i{itemNumber}];{GenerateComparerProp(array.ElementType, $"item{itemNumber}", $"otherItem{itemNumber}", $"{indent}    ", generic)}
{indent}}}";
        }

        // Record
        if (underlyingType.IsRecord || underlyingType.TypeKind == TypeKind.Struct)
            return $@"
{indent}if ({propName} != {otherPropName}) return true;";

        if (underlyingType is INamedTypeSymbol named)
            NeededComparers.Add(named);

        // Niet array !!!!!
        Reg(underlyingType.ContainingNamespace.ToDisplayString());
        return isNullable
            ? $@"
{indent}if ({propName} is null)
{indent}{{
{indent}    if ({otherPropName} is not null) return true;
{indent}}}
{indent}else
{indent}{{
{indent}    if ({otherPropName} is null) return true;
{indent}    if ({propName}.IsDifferent({otherPropName})) return true;
{indent}}}"
            : $@"
{indent}if ({propName}.IsDifferent({otherPropName})) return true;";
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