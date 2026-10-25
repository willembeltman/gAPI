using gAPI.AutoBus.Helpers;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoBus.Models;

public class ServiceContext
{
    public ServiceContext(INamedTypeSymbol[] allSymbols)
    {
        var busInterfaceSymbols = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Interface &&
                t.HasAttribute("gAPI.Core.Attributes.GenerateBusAttribute"))
            .ToArray();

        BusInterfaces = busInterfaceSymbols
            .Select(interfaceSymbol => new Interface(this, interfaceSymbol, allSymbols))
            //.Where(a => a.Service == null)
            .OrderBy(a => a.Name)
            .ToArray();
    }

    public Interface[] BusInterfaces { get; }

    public List<string> CheckForErrors()
    {
        var errors = new List<string>();

        foreach (var hubInterface in BusInterfaces)
            foreach (var method in hubInterface.Methods)
                CheckHub(method.ResponseType, errors, method.Name, hubInterface.FullName);

        return errors;
    }
    private void CheckHub(TypeHelper responseType, List<string> errors, string method, string hubInterface)
    {
        //if (responseType.IsTaskT)
        //{
        //    errors.Add(
        //        $"Method '{method}' on interface '{hubInterface}' returns Task<T>. " +
        //        "Please use IAsyncEnumerable<T> instead. " +
        //        "When communicating from server to client, the number of client responses is unknown, " +
        //        "therefore response methods must use IAsyncEnumerable<T>.");
        //}
        //else 
        if (!responseType.IsTaskT && !responseType.IsTask && !responseType.IsIAsyncEnumerable && !responseType.IsVoid)
        {
            errors.Add(
                $"Method '{method}' on interface '{hubInterface}' appears to be synchronous. " +
                "Please use Task (no response) or IAsyncEnumerable<T> (with responses).");
        }
    }
}
