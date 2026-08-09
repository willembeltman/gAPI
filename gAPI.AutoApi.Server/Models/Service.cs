using Microsoft.CodeAnalysis;

namespace gAPI.AutoApi.Server.Models;

public class Service : SharedReference
{
    public Service(Interface @interface, INamedTypeSymbol a) : base(a)
    {
    }
}