using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoWss.Server.Models;

public class Service : SharedReference
{
    public Service(Interface @interface, INamedTypeSymbol namedTypeSymbol) : base(namedTypeSymbol)
    {
        IsApiProxy = namedTypeSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsApiProxyAttribute");
        IsHubProxy = namedTypeSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsHubProxyAttribute");
    }

    public bool IsApiProxy { get; }
    public bool IsHubProxy { get; }
}