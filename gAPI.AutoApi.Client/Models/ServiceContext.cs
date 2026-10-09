using gAPI.AutoApi.Client.Helpers;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoApi.Client.Models;

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
            .OrderBy(a => a.Name)
            .ToArray();

        var apiInterfaceSymbols = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Interface &&
                t.HasAttribute("gAPI.Core.Attributes.GenerateApiAttribute"))
            .ToArray();

        ApiInterfaces = apiInterfaceSymbols
            .Select(interfaceSymbol => new Interface(this, interfaceSymbol, allSymbols))
            .OrderBy(a => a.Name)
            .ToArray();

        var minimalApiInterfaceSymbols = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Interface &&
                t.HasAttribute("gAPI.Core.Attributes.GenerateMinimalApiAttribute"))
            .ToArray();

        MinimalApiInterfaces = minimalApiInterfaceSymbols
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
                CheckHub(method.ResponseType, errors, method.Name, hubInterface.FullName);

        foreach (var apiInterface in ApiInterfaces)
            foreach (var method in apiInterface.Methods)
                CheckApi(method, apiInterface, errors);

        foreach (var apiInterface in MinimalApiInterfaces)
            foreach (var method in apiInterface.Methods)
                CheckApi(method, apiInterface, errors);

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
        else if (!responseType.IsTask && !responseType.IsIAsyncEnumerable && !responseType.IsVoid)
        {
            errors.Add(
                $"Method '{method}' on interface '{hubInterface}' appears to be synchronous. " +
                "Please use Task (no response) or IAsyncEnumerable<T> (with responses).");
        }
    }


    private void CheckApi(InterfaceMethod method, Interface apiInterface, List<string> errors)
    {
        //var args = method.Arguments.Where(a => a.IsIEnumerable).ToArray();
        //foreach (var arg in args) 
        //{
        //    errors.Add(
        //        $"Method '{method}' on interface '{apiInterface}' '{arg}' " +
        //        "This is not supported.");

        //}
        if (!method.ResponseType.IsTaskT && !method.ResponseType.IsTask && !method.ResponseType.IsVoid)
        {
            errors.Add(
                $"Method '{method}' on interface '{apiInterface}' appears to not be supported. " +
                "Please use Task (no response), Task<T> or IAsyncEnumerable<T> (with responses).");
        }
    }
}
