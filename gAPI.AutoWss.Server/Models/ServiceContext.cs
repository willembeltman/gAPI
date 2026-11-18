using gAPI.AutoWss.Server.Helpers;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoWss.Server.Models;

public class ServiceContext
{
    public ServiceContext(INamedTypeSymbol[] allSymbols)
    {
        var hubInterfaceSymbols = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Interface &&
                t.HasAttribute("gAPI.Core.Attributes.GenerateHubAttribute"))
            .ToArray();

        HubInterfaces = hubInterfaceSymbols
            .Select(interfaceSymbol => new Interface(this, interfaceSymbol, allSymbols))
            .Where(a => a.Service?.IsHubProxy != true)
            .OrderBy(a => a.Name)
            .ToArray();

        var apiInterfaceSymbols = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Interface &&
                t.HasAttribute("gAPI.Core.Attributes.GenerateApiAttribute"))
            .ToArray();

        ApiInterfaces = apiInterfaceSymbols
            .Select(interfaceSymbol => new Interface(this, interfaceSymbol, allSymbols))
            .Where(a => a.Service != null)
            .OrderBy(a => a.Name)
            .ToArray();

        var minimalApiInterfaceSymbols = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Interface &&
                t.HasAttribute("gAPI.Core.Attributes.GenerateMinimalApiAttribute"))
            .OrderBy(a => a.Name)
            .ToArray();

        MinimalApiInterfaces = minimalApiInterfaceSymbols
            //.Where(a => a.ToDisplayString() != "gAPI.Core.Interfaces.IAccountService")
            .Select(interfaceSymbol => new Interface(this, interfaceSymbol, allSymbols))
            .OrderBy(a => a.Name)
            .ToArray();
    }

    public Interface[] HubInterfaces { get; }
    public Interface[] ApiInterfaces { get; }
    public Interface[] MinimalApiInterfaces { get; }

    public List<string> CheckForErrors()
    {
        var errors = new List<string>();

        foreach (var hubInterface in HubInterfaces)
            foreach (var method in hubInterface.Methods)
                CheckHub(method.ResponseType, errors, method, hubInterface.FullName);

        foreach (var hubInterface in ApiInterfaces)
            foreach (var method in hubInterface.Methods)
                CheckApi(method.ResponseType, errors, method, hubInterface.FullName);

        foreach (var hubInterface in MinimalApiInterfaces)
            foreach (var method in hubInterface.Methods)
                CheckApi(method.ResponseType, errors, method, hubInterface.FullName);

        return errors;
    }

    private void CheckHub(TypeHelper responseType, List<string> errors, InterfaceMethod method, string hubInterface)
    {
        var methodIdentifier = $"Method '{method}' on interface '{hubInterface}'";
        CheckVoidWithAsyncEnumerable(responseType, errors, method, methodIdentifier);

        if (responseType.IsTaskT)
        {
            errors.Add(
                $"{methodIdentifier} returns Task<T>, which is not supported. Please use IAsyncEnumerable<T> instead. " +
                "Because the number of client responses is unknown in server-to-client communication, " +
                "methods with responses must return IAsyncEnumerable<T>.");
            return;
        }

        if (!responseType.IsTask && !responseType.IsIAsyncEnumerable && !responseType.IsVoid)
        {
            errors.Add(
                $"{methodIdentifier} has an unsupported return type. " +
                "Please use void, Task (no response), or IAsyncEnumerable<T> (with responses).");
        }
    }

    private void CheckApi(TypeHelper responseType, List<string> errors, InterfaceMethod method, string apiInterface)
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
