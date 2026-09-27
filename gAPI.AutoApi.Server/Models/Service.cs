using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoApi.Server.Models;

public class Service : SharedReference
{
    public Service(Interface @interface, INamedTypeSymbol namedTypeSymbol) : base(namedTypeSymbol)
    {
        IsApiProxy = namedTypeSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsApiProxyAttribute");
    }

    public bool IsApiProxy { get; }
}