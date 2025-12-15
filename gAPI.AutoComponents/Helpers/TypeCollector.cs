using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoComponents.Helpers
{
    public class TypeCollector
    {
        //public static string[] SkipTypes = new[]
        //{
        //    "System.Object",
        //    "System.String",
        //    "System.Int32",
        //    "System.Int64",
        //    "System.Boolean",
        //    "System.Double",
        //    "System.Decimal",
        //    "System.DateTime",
        //    "System.Guid",
        //    "long",
        //    "int",
        //    "long?",
        //    "bool",
        //    "string?",
        //    "char",
        //    "int?",
        //    "nint",
        //    "System.Threading.Tasks.Task<TResult>",
        //    "System.Threading.Tasks.TaskFactory<TResult>",
        //    "System.Threading.CancellationToken",
        //    "System.Threading.WaitHandle",
        //    "Microsoft.Win32.SafeHandles.SafeWaitHandle",
        //    "System.Threading.Tasks.TaskScheduler?",
        //};
        //public static string[] SkipBaseTypes = new[]
        //{
        //    "System.Threading.Tasks.Task<TResult>",
        //    "System.Threading.Tasks.TaskFactory<TResult>",
        //    "BSD.Shared.ResponseDtos.BaseResponse<T>",
        //    "BSD.Shared.ResponseDtos.BaseListResponse<T>"
        //};

        private readonly HashSet<ITypeSymbol> Seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        private readonly HashSet<ITypeSymbol> Added = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        public TypeCollector(Configs.ClientConfig config)
        {
            Config = config;
        }

        public List<INamedTypeSymbol> Dtos { get; } = new List<INamedTypeSymbol>();
        public List<INamedTypeSymbol> Enums { get; } = new List<INamedTypeSymbol>();
        //public List<INamedTypeSymbol> Services { get; } = new List<INamedTypeSymbol>();
        public Configs.ClientConfig Config { get; }

        public void Add(ITypeSymbol type)
        {
            if (!Seen.Add(type))
                return;

            switch (type)
            {
                case INamedTypeSymbol named when named.TypeKind == TypeKind.Enum:

                    if (IsValidToAdd(named))
                        Enums.Add(named);
                    break;

                case INamedTypeSymbol named when named.TypeKind == TypeKind.Class || named.TypeKind == TypeKind.Struct:
                    //if (!SkipTypes.Contains(named.ToDisplayString()) &&
                    //    !SkipBaseTypes.Contains(named.OriginalDefinition.ToDisplayString()))

                    if (IsValidToAdd(named.OriginalDefinition))
                        Dtos.Add(named.OriginalDefinition);

                    foreach (var prop in named.GetMembers().OfType<IPropertySymbol>()
                        .Where(p => p.DeclaredAccessibility == Accessibility.Public))
                    {
                        Add(prop.Type);
                    }

                    if (named.IsGenericType)
                    {
                        foreach (var arg in named.TypeArguments)
                            Add(arg);
                    }

                    break;

                case IArrayTypeSymbol array:
                    Add(array.ElementType);
                    break;

                case INamedTypeSymbol gen when gen.IsGenericType:
                    foreach (var arg in gen.TypeArguments)
                        Add(arg);
                    break;
            }
        }

        private bool IsValidToAdd(ITypeSymbol type)
        {
            return
                type != null &&
                Added.Add(type) &&
                type.ContainingNamespace != null &&
                type.ContainingNamespace.ToDisplayString() != null &&
                Config.BaseNamespaces.Any(a => type.ContainingNamespace.ToDisplayString().StartsWith(a));
        }

    }

}