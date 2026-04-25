using Microsoft.CodeAnalysis;

namespace gAPI.AutoWssServer.Models;

public class Service : SharedReference
{
    public Service(Interface @interface, INamedTypeSymbol a) : base(a)
    {
    }
}