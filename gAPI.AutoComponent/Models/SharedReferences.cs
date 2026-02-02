using gAPI.AutoComponent.Helpers;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoComponent.Models;

public class SharedReferences
{
    public SharedReferences(Compilation compilation, INamedTypeSymbol[] allSymbols)
    {
        AllComponents = [.. allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                !t.IsAbstract &&
                t.BaseType != null &&
                InheritsFromComponentBase(t))
            .Select(a => new SharedReference(a))];

        IsFormFileExtention = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                t.HasAttribute("gAPI.Attributes.IsFormFileExtentionAttribute"))
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new InvalidOperationException(
                "Please add gAPI.AutoClient. IsFormFileExtention helper is missing.");

        FormFile = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                t.HasAttribute("gAPI.Attributes.IsFormFileAttribute"))
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new InvalidOperationException(
                "Please add gAPI.AutoClient. FormFile is missing.");

        State = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                t.HasAttribute("gAPI.Attributes.IsStateDtoAttribute"))
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new InvalidOperationException(
                "The `State` dto is missing or does not have the required " +
                "`gAPI.Attributes.IsStateDtoAttribute`. " +
                "Ensure your shared project defines a `State` dto and that it is annotated with " +
                "`[IsStateDtoAttribute]`.");

        var StateChangedHandler_Symbol =
            compilation.GetTypeByMetadataName("gAPI.Delegates.StateChangedHandler") ??
            throw new Exception("gAPI.Delegates.StateChangedHandler was not found. " +
                "Please reference the gAPI package on the same project as gAPI.AutoComponents references.");
        StateChangedHandler = new SharedReference(StateChangedHandler_Symbol);

        var baseResponse_Symbol =
            compilation.GetTypeByMetadataName("gAPI.Dtos.BaseResponse") ??
            throw new Exception("gAPI.Dtos.BaseResponse was not found. " +
                "Please reference the gAPI package on the same project as gAPI.AutoComponents references.");
        BaseResponse = new SharedReference(baseResponse_Symbol);
        var baseResponseT_Symbol =
            compilation.GetTypeByMetadataName("gAPI.Dtos.BaseResponseT`1") ??
            throw new Exception("gAPI.Dtos.BaseResponseT was not found. " +
                "Please reference the gAPI package on the same project as gAPI.AutoComponents references.");
        BaseResponseT = new SharedReference(baseResponseT_Symbol);
        var baseListResponseT_Symbol =
            compilation.GetTypeByMetadataName("gAPI.Dtos.BaseListResponseT`1") ??
            throw new Exception("gAPI.Dtos.BaseListResponseT was not found. " +
                "Please reference the gAPI package on the same project as gAPI.AutoComponents references.");
        BaseListResponseT = new SharedReference(baseListResponseT_Symbol);


        var itemDataSourceSymbol =
            allSymbols
                .FirstOrDefault(t =>
                    t.TypeKind == TypeKind.Class &&
                    t.Name == "ItemDataSource" &&
                    t.Arity == 2);
        if (itemDataSourceSymbol != null)
        {
            ItemDataSource = new SharedReference(itemDataSourceSymbol);
        }

        var listDataSourceSymbol =
            allSymbols
                .FirstOrDefault(t =>
                    t.TypeKind == TypeKind.Class &&
                    t.Name == "ListDataSource" &&
                    t.Arity == 2);
        if (listDataSourceSymbol != null)
        {
            ListDataSource = new SharedReference(listDataSourceSymbol);
        }


        var gAPI_IClientAuthenticationService_Symbol =
            compilation.GetTypeByMetadataName("gAPI.Interfaces.IClientAuthenticationService")
            ?? throw new Exception("gAPI.Interfaces.IClientAuthenticationService was not found. " +
                "Please reference the gAPI package on the same project as gAPI.AutoComponents references.");

        Gapi_IClientAuthenticationService = new SharedReference(gAPI_IClientAuthenticationService_Symbol);
        var iClientAuthenticationService_Symbol =
            allSymbols
                .Where(t =>
                    t.TypeKind == TypeKind.Interface &&
                    t.AllInterfaces.Any(i =>
                        SymbolEqualityComparer.Default.Equals(i, gAPI_IClientAuthenticationService_Symbol)))
                .FirstOrDefault();

        if (iClientAuthenticationService_Symbol != null)
        {
            IClientAuthenticationService = new SharedReference(iClientAuthenticationService_Symbol);

            var clientAuthenticationService_Symbol =
                allSymbols
                    .Where(t =>
                        t.TypeKind == TypeKind.Class &&
                        t.AllInterfaces.Any(i =>
                            SymbolEqualityComparer.Default.Equals(i, iClientAuthenticationService_Symbol)))
                    .FirstOrDefault();

            if (clientAuthenticationService_Symbol != null)
                ClientAuthenticationService = new SharedReference(clientAuthenticationService_Symbol);
        }

        ErrorView = AllComponents.FirstOrDefault(a => a.Name == "ErrorView");
        LoaderView = AllComponents.FirstOrDefault(a => a.Name == "LoaderView");
        RedirectToHome = AllComponents.FirstOrDefault(a => a.Name == "RedirectToHome");
        RedirectToLogin = AllComponents.FirstOrDefault(a => a.Name == "RedirectToLogin");
    }
    bool InheritsFromComponentBase(INamedTypeSymbol symbol)
    {
        var current = symbol.BaseType;
        while (current != null)
        {
            if (current.ToDisplayString() == "Microsoft.AspNetCore.Components.ComponentBase")
                return true;
            current = current.BaseType;
        }
        return false;
    }

    public SharedReference[] AllComponents { get; }
    public SharedReference Gapi_IClientAuthenticationService { get; }
    public SharedReference IsFormFileExtention { get; }
    public SharedReference FormFile { get; }
    public SharedReference State { get; }
    public SharedReference BaseResponse { get; }
    public SharedReference BaseResponseT { get; }
    public SharedReference BaseListResponseT { get; }
    public SharedReference StateChangedHandler { get; }
    public SharedReference? IClientAuthenticationService { get; }
    public SharedReference? ClientAuthenticationService { get; }
    public SharedReference? ItemDataSource { get; }
    public SharedReference? ListDataSource { get; }
    public SharedReference? ErrorView { get; }
    public SharedReference? LoaderView { get; }
    public SharedReference? RedirectToHome { get; }
    public SharedReference? RedirectToLogin { get; }
}