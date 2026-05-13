using gAPI.AutoWssServer.Helpers;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoWssServer.Models;

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
            .ToArray();

        var apiInterfaceSymbols = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Interface &&
                t.HasAttribute("gAPI.Core.Attributes.GenerateApiAttribute"))
            .ToArray();

        ApiInterfaces = apiInterfaceSymbols
            .Select(interfaceSymbol => new Interface(this, interfaceSymbol, allSymbols))
            .ToArray();

        var minimalApiInterfaceSymbols = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Interface &&
                t.HasAttribute("gAPI.Core.Attributes.GenerateMinimalApiAttribute"))
            .ToArray();

        MinimalApiInterfaces = minimalApiInterfaceSymbols
            .Where(a => a.ToDisplayString() != "gAPI.Core.Interfaces.IAccountService")
            .Select(interfaceSymbol => new Interface(this, interfaceSymbol, allSymbols))
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
                CheckHub(method.ResponseType, errors, method.Name, hubInterface.FullName);

        foreach (var hubInterface in ApiInterfaces)
            foreach (var method in hubInterface.Methods)
                CheckApi(method.ResponseType, errors, method.Name, hubInterface.FullName);

        foreach (var hubInterface in MinimalApiInterfaces)
            foreach (var method in hubInterface.Methods)
                CheckApi(method.ResponseType, errors, method.Name, hubInterface.FullName);

        return errors;
    }
    private void CheckHub(TypeHelper responseType, List<string> errors, string method, string hubInterface)
    {
        if (responseType.IsTaskT)
        {
            errors.Add(
                $"Method '{method}' on interface '{hubInterface}' returns Task<T>. " +
                "Please use IAsyncEnumerable<T> instead. " +
                "When communicating from server to client, the number of client responses is unknown, " +
                "therefore response methods must use IAsyncEnumerable<T>.");
        }
        else if (!responseType.IsTask && !responseType.IsIAsyncEnumerable)
        {
            errors.Add(
                $"Method '{method}' on interface '{hubInterface}' appears to be synchronous. " +
                "Please use Task (no response) or IAsyncEnumerable<T> (with responses).");
        }
    }
    private void CheckApi(TypeHelper responseType, List<string> errors, string method, string hubInterface)
    {
        if (!responseType.IsTaskT && !responseType.IsTask && !responseType.IsIAsyncEnumerable)
        {
            errors.Add(
                $"Method '{method}' on interface '{hubInterface}' appears to be synchronous. " +
                "Please use Task (no response), Task<T> or IAsyncEnumerable<T> (with responses).");
        }
    }
}
