using Microsoft.CodeAnalysis;
using System.Linq;

namespace gAPI.AutoWss.Client.Models;

public class Service : SharedReference
{
    public Service(Interface @interface, INamedTypeSymbol namedTypeSymbol) : base(namedTypeSymbol)
    {
        IsHubProxy = namedTypeSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "IsHubProxyAttribute");

        IsHostedHubService = namedTypeSymbol.AllInterfaces
            .Any(i => i.ToDisplayString() == "gAPI.Core.Client.Interfaces.IHostedHubService");
    }

    public bool IsHubProxy { get; }
    public bool IsHostedHubService { get;}
}