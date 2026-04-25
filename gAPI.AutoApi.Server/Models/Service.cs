using Microsoft.CodeAnalysis;

namespace gAPI.AutoApiServer.Models;

public class Service : SharedReference
{
    public Service(Interface @interface, INamedTypeSymbol a) : base(a)
    {
    }
}