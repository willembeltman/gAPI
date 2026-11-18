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
                CheckBus(method.ResponseType, errors, method, hubInterface.FullName);

        return errors;
    }

    private void CheckBus(TypeHelper responseType, List<string> errors, InterfaceMethod method, string apiInterface)
    {
        var methodIdentifier = $"Method '{method}' on interface '{apiInterface}'";
        CheckVoidWithAsyncEnumerable(responseType, errors, method, methodIdentifier);

        if (!responseType.IsTaskT && !responseType.IsTask && !responseType.IsIAsyncEnumerable && !responseType.IsVoid)
        {
            errors.Add(
                $"{methodIdentifier} has an unsupported return type. " +
                "Please use void or Task (no response), or, Task<T> or IAsyncEnumerable<T> (with responses).");
        }
    }

    private static void CheckVoidWithAsyncEnumerable(
        TypeHelper responseType,
        List<string> errors,
        InterfaceMethod method,
        string methodIdentifier)
    {
        var hasAsyncEnumerableArg = method.Arguments.Any(a => a.ParameterType.IsIAsyncEnumerable);
        if (responseType.IsVoid && hasAsyncEnumerableArg)
        {
            errors.Add(
                $"{methodIdentifier} accepts an IAsyncEnumerable argument while returning void. " +
                "You must await the method until completion (by returning Task); otherwise, " +
                "the CancellationTokenSource timeout will be disposed before streaming starts.");
        }
    }
}
