using Microsoft.CodeAnalysis;

namespace gAPI.AutoWss.Server.Models;

public class Service : SharedReference
{
    public Service(Interface @interface, INamedTypeSymbol a) : base(a)
    {
    }
}