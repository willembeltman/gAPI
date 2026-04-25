using Microsoft.CodeAnalysis;

namespace gAPI.AutoSseServer.Models;

public class Service : SharedReference
{
    public Service(Interface @interface, INamedTypeSymbol a) : base(a)
    {
    }
}