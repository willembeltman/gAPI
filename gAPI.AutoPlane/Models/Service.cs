using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoPlane.Models;

public class Service : SharedReference
{
    public Service(Interface @interface, INamedTypeSymbol namedTypeSymbol) : base(namedTypeSymbol)
    {
        IsApiProxy = namedTypeSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsApiProxyAttribute");

        IsHostedPlaneService = namedTypeSymbol.AllInterfaces
            .Any(i => i.ToDisplayString() == "gAPI.Core.Server.Interfaces.IHostedPlaneService");
    }

    public bool IsApiProxy { get; }
    public bool IsHostedPlaneService { get; }
}