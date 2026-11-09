using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoBus.Models;

public class Service : SharedReference
{
    public Service(Interface @interface, INamedTypeSymbol namedTypeSymbol) : base(namedTypeSymbol)
    {
        IsApiProxy = namedTypeSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsApiProxyAttribute");

        IsHostedBusService = namedTypeSymbol.AllInterfaces
            .Any(i => i.ToDisplayString() == "gAPI.Core.Server.Interfaces.IHostedBusService");
    }

    public bool IsApiProxy { get; }
    public bool IsHostedBusService { get; }
}