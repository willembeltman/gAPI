using gAPI.AutoSseClient.Configs;
using gAPI.AutoSseClient.Helpers;
using gAPI.AutoSseClient.Models;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoSseClient
{
    internal class ServiceContext
    {
        internal ServiceContext(Compilation compilation, ClientConfig config)
        {
            Config = config ?? throw new Exception("ClientConfig cannot be null");

            var allSymbols = compilation.GlobalNamespace.GetAllTypes();
            var interfaceSymbols = allSymbols
                .Where(t =>
                    t.TypeKind == TypeKind.Interface &&
                    t.HasAttribute("gAPI.Attributes.GenerateHubAttribute") &&
                    Config.BaseNamespaces.Any(a => t.ContainingNamespace.ToDisplayString().StartsWith(a)))
                .ToArray();

            Interfaces = interfaceSymbols
                .Select(interfaceSymbol => new Interface(this, interfaceSymbol, allSymbols))
                .ToArray();

            var collector = new TypeCollector(config);
            foreach (var swi in interfaceSymbols)
            {
                foreach (var method in swi.GetMembers().OfType<IMethodSymbol>())
                {
                    if (method.DeclaredAccessibility != Accessibility.Public)
                        continue;

                    foreach (var param in method.Parameters)
                    {
                        collector.Add(param.Type);
                    }

                    collector.Add(method.ReturnType);
                }
            }

            Enums = collector.Enums
                .Select(namedTypeSymbol => new EnumDto(namedTypeSymbol))
                .ToArray();
            Dtos = collector.Dtos
                .Select(namedTypeSymbol => new Dto(this, namedTypeSymbol))
                .ToArray();

            IClientAuthenticationService = Find("gAPI.Interfaces.IClientAuthenticationService", allSymbols);
            ISseManagerBase = Find("gAPI.Interfaces.ISseManagerBase", allSymbols);
            SseServiceId = Find("gAPI.Ids.SseServiceId", allSymbols);
            SseHostId = Find("gAPI.Ids.SseHostId", allSymbols);
            SseMessage = Find("gAPI.Sse.SseMessage", allSymbols);
            SseManagerCollection = Find("gAPI.Sse.SseManagerCollection", allSymbols);
        }
        private SharedReference Find(string typeFullName, IEnumerable<INamedTypeSymbol> allSymbols)
        {
            foreach (var symbol in allSymbols)
            {
                if (IsExactType(symbol, typeFullName))
                    return new SharedReference(symbol);
            }

            throw new Exception($"Cannot find type '{typeFullName}'");
        }

        private static readonly SymbolDisplayFormat FullNameFormat =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static bool IsExactType(INamedTypeSymbol symbol, string fullName)
        {
            return symbol.ToDisplayString(FullNameFormat) == fullName;
        }


        public ClientConfig Config { get; }
        public Interface[] Interfaces { get; }
        public EnumDto[] Enums { get; }
        public Dto[] Dtos { get; }
        public SharedReference IClientAuthenticationService { get; }
        public SharedReference ISseManagerBase { get; }
        public SharedReference SseServiceId { get; }
        public SharedReference SseHostId { get; }
        public SharedReference SseManagerCollection { get; }
        public SharedReference SseMessage { get; internal set; }
    }
}
