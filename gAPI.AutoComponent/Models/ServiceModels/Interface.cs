using gAPI.AutoComponent.Contexts;
using gAPI.AutoComponent.Helpers;
using gAPI.AutoComponent.Interfaces;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoComponent.Models.ServiceModels
{
    public class Interface : ISharedReference
    {
        public Interface(ServiceContext serviceContext, INamedTypeSymbol namedTypeSymbol, IEnumerable<INamedTypeSymbol> allSymbols)
        {
            NamedTypeSymbol = namedTypeSymbol;

            Name = NamedTypeSymbol.Name;
            FullName = NamedTypeSymbol.ToDisplayString();
            Namespace = NamedTypeSymbol.ContainingNamespace.ToDisplayString();

            ApiName = Name;
            ApiName = ServiceNameHelper.RemoveInterfacePrefix(ApiName);
            var apiNameAttr = NamedTypeSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "ApiNameAttribute");
            if (apiNameAttr != null)
            {
                ApiName = apiNameAttr.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? ApiName;
            }
            ApiName = ServiceNameHelper.RemoveServiceName(ApiName);

            IsAuthorized = NamedTypeSymbol.GetAttributes()
                .Any(a => a.AttributeClass?.Name == "IsAuthorizedAttribute");

            IsHidden = NamedTypeSymbol.GetAttributes()
                .Any(a => a.AttributeClass?.Name == "IsHiddenAttribute");

            Methods = NamedTypeSymbol
                .GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.MethodKind == MethodKind.Ordinary)
                .Where(m => !m.GetAttributes().Any(attr => attr.AttributeClass?.Name == "IsHiddenAttribute"))
                .Select(methodSymbol => new InterfaceMethod(serviceContext, this, methodSymbol))
                .ToArray();

            Client = allSymbols
                .Where(a =>
                    a.TypeKind == TypeKind.Class &&
                    a.Interfaces.Any(@interface => @interface.ToDisplayString() == namedTypeSymbol.ToDisplayString()))
                .Select(a => new Client(this, a))
                .SingleOrDefault();
        }

        public INamedTypeSymbol NamedTypeSymbol { get; }
        public string Name { get; }
        public string FullName { get; }
        public string Namespace { get; }
        public string ApiName { get; }
        public bool IsAuthorized { get; }
        public bool IsHidden { get; }
        public InterfaceMethod[] Methods { get; }
        public Client Client { get; }
    }
}