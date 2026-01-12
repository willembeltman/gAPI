using gAPI.AutoComparer.Helpers;
using gAPI.Helpers;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoComparer.Engine;

internal static class ComparerFactory<TIn, TOut>
{
    private const string Tabs = "\t\t\t";
    private const string LineReturn = "\r\n";

    public static ComparerInstance<TIn, TOut> CreateInstance()
    {
        var typeIn = typeof(TIn);
        var typeOut = typeof(TOut);
        var className = $"{typeIn.Name}{typeOut.Name}Comparer";
        var @namespace = "gAPI.AutoComparer.GeneratedComparers";
        var isDirtyMethodName = "Compare";
        var fullClassName = $"{@namespace}.{className}";

        var code = GenerateClass(typeIn, typeOut, className, @namespace, isDirtyMethodName);

        var asm = CodeCompiler.Compile(code);
        var serializerType = asm.GetType(fullClassName)!;
        var isDirtyMethod = serializerType.GetMethod(isDirtyMethodName)!;

        var isDirtyDelegate = (Func<TIn, TOut, bool>)Delegate.CreateDelegate(
            typeof(Func<TIn, TOut, bool>), isDirtyMethod);

        return new ComparerInstance<TIn, TOut>(code, isDirtyDelegate);
    }

    private static string GenerateClass(Type typeIn, Type typeOut, string className, string @namespace, string mapMethodName)
    {
        var propsIn = typeIn.GetProperties();
        var propsOut = typeOut.GetProperties();

        return $@"
using System;
using System.Linq;
using System.Threading.Tasks;

#nullable disable
namespace {@namespace}
{{
    public static class {className}
    {{
        public static bool {mapMethodName}({typeIn.FullName} source, {typeOut.FullName} destination)
        {{
		    if (source == null && destination == null) return true;
		    if (source == null && destination != null) return false;
		    if (source != null && destination == null) return false;
		    {GenerateInnerClass(typeIn, typeOut)}
            return true;
        }}       
    }}
}}";
    }

    private static string GenerateInnerClass(Type typeIn, Type typeOut)
    {
        var mapCode = string.Empty;

        var propsIn = typeIn.GetProperties();
        var propsOut = typeOut.GetProperties();

        foreach (var propIn in propsIn)
        {
            var propOut = propsOut.FirstOrDefault(a => a.Name == propIn.Name);
            if (propOut == null) continue;

            if (!ReflectionHelper.HasPublicGetter(propIn)) continue;
            if (!ReflectionHelper.HasPublicSetter(propOut)) continue;

            mapCode +=
                LineReturn +
                Tabs +
                GenerateConversionCode(
                    new TypeComparerInfo(propIn.PropertyType),
                    new TypeComparerInfo(propOut.PropertyType),
                    $"source.{propIn.Name}",
                    $"destination.{propOut.Name}");
        }

        return mapCode;
    }

    private static string GenerateConversionCode(TypeComparerInfo sourceType, TypeComparerInfo targetType, string sourceExpression, string targetExpression)
    {
        // Directe toewijzing
        if (targetType == sourceType)
            return $"if ({targetExpression} != {sourceExpression}) return false;";

        // Enum conversie
        if (targetType.IsEnum)
            return GenerateEnumCompare(sourceType, targetType, sourceExpression, targetExpression);

        // Collection mapping
        if (targetType.IsIEnumerable)
            return GenerateCollectionCompare(sourceType, targetType, sourceExpression, targetExpression);

        // Complexe type (recursief)
        if (targetType.IsComplex)
            return GenerateComplexCompare(targetType, sourceExpression, targetExpression);

        // Primative mapping
        return GeneratePrimativeCompare(sourceType, targetType, sourceExpression, targetExpression);
    }

    private static string GenerateEnumCompare(TypeComparerInfo sourceType, TypeComparerInfo targetType, string sourceExpression, string targetExpression)
    {
        if (sourceType == typeof(string))
            return $"if ({targetExpression} != ({targetType})Enum.Parse(typeof({targetType}), {sourceExpression}, true)) return false;";

        if (sourceType == typeof(int))
            return $"if ({targetExpression} != ({targetType}){sourceExpression}) return false;";

        return $"if ({targetExpression} != ({targetType})Enum.ToObject(typeof({targetType}), {sourceExpression})) return false;";
    }

    private static string GenerateCollectionCompare(TypeComparerInfo sourceType, TypeComparerInfo targetType, string sourceExpr, string targetExpr)
    {
        var srcElem = new TypeComparerInfo(sourceType.ElementType);
        var dstElem = new TypeComparerInfo(targetType.ElementType);

        var mapExpr = GenerateElementCompareExpression(srcElem, dstElem, "x");

        var collectionAssignment = targetType.IsArray
            ? $"var tmp = {sourceExpr}.Select(x => {mapExpr}).ToArray();"
            : $"var tmp = {sourceExpr}.Select(x => {mapExpr}).ToList();";

        return $@"
if ({sourceExpr} != null)
{{
    {collectionAssignment}
    if ({targetExpr} != tmp) return false;
}}";
    }
    private static string GenerateElementCompareExpression(TypeComparerInfo sourceType, TypeComparerInfo targetType, string sourceExpr)
    {
        // Als elementtypes gelijk zijn
        if (sourceType == targetType)
            return $"({targetType.ElementType.FullName}){sourceExpr}";

        // Als elementtype een complex object is
        if (!targetType.ElementType.IsPrimitive && targetType.ElementType != typeof(string))
        {
            return $@"({sourceExpr} != null ? {sourceExpr}.CompareTo(new {targetType.ElementType.FullName}()) : null)";
        }

        // Default cast
        return $"({targetType.ElementType.FullName}){sourceExpr}";
    }

    private static string GenerateComplexCompare(TypeComparerInfo targetType, string sourceExpression, string targetExpression)
    {
        return $@"
if ({sourceExpression} != null)
{{
    if ({targetExpression} == null)
        {targetExpression} = new {targetType.ElementType.FullName}();
    if (!gAPI.AutoComparer.Comparer.Compare({sourceExpression}, {targetExpression})) return false;
}}";
    }

    private static string GeneratePrimativeCompare(TypeComparerInfo sourceType, TypeComparerInfo targetType, string sourceExpression, string targetExpression)
    {
        // DateTime -> string
        if (targetType == typeof(string) && sourceType == typeof(DateTime))
            return $"if ({targetExpression} != {sourceExpression}.ToString(\"o\")) return false;";

        // string -> Guid
        if (targetType == typeof(Guid) && sourceType == typeof(string))
            return $"if ({targetExpression} != Guid.Parse({sourceExpression})) return false;";

        // Default cast
        return $"if ({targetExpression} != ({targetType.ElementType.FullName})System.Convert.ChangeType({sourceExpression}, typeof({targetType.ElementType.FullName}))) return false;";
    }
}
