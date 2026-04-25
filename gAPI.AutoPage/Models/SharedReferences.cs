using gAPI.AutoPage.Helpers;
using gAPI.AutoPage.Interfaces;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace gAPI.AutoPage.Models;

public class SharedReferences : ISharedReferences
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

        FormFileExtension = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                t.HasAttribute("gAPI.Attributes.IsFormFileExtensionAttribute"))
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new InvalidOperationException(
                "Please add gAPI.AutoClient. IsFormFileExtension helper is missing.");

        FormFile = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                t.HasAttribute("gAPI.Attributes.IsFormFileAttribute"))
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new InvalidOperationException(
                "Please add gAPI.AutoClient. FormFile is missing.");

        static int InheritsFrom(INamedTypeSymbol? symbol, INamedTypeSymbol baseType)
        {
            var count = 0;
            while (symbol != null)
            {
                if (SymbolEqualityComparer.Default.Equals(symbol, baseType))
                    return count;

                symbol = symbol.BaseType;
                count++;
            }
            return -1;
        }

        var stateBaseTypes = allSymbols
            .Where(t =>
                t.TypeKind == TypeKind.Class &&
                t.HasAttribute("gAPI.Attributes.IsStateDtoAttribute"))
            .Cast<INamedTypeSymbol>()
            .ToArray();

        var stateObjects = allSymbols
            .OfType<INamedTypeSymbol>()
            .Where(t => t.TypeKind == TypeKind.Class)
            .Select(t => new { type = t, count = stateBaseTypes.Max(baseType => InheritsFrom(t.BaseType, baseType)) })
            .OrderByDescending(a => a.count)
            .Select(a => a.type)
            .ToArray();

        State = stateObjects
            .Select(a => new SharedReference(a))
            .FirstOrDefault() ?? throw new InvalidOperationException(
                "The `State` dto is missing or does not have the required " +
                "`gAPI.Attributes.IsStateDtoAttribute`. " +
                "Ensure your shared project defines a `State` dto and that it is annotated with " +
                "`[IsStateDtoAttribute]`.");

        var StateChangedHandler_Symbol =
            compilation.GetTypeByMetadataName("gAPI.Delegates.StateChangedHandler") ??
            throw new Exception("gAPI.Delegates.StateChangedHandler was not found. " +
                "Please reference the gAPI package on the same project as gAPI.AutoPages references.");
        StateChangedHandler = new SharedReference(StateChangedHandler_Symbol);

        var baseResponse_Symbol =
            compilation.GetTypeByMetadataName("gAPI.Dtos.BaseResponse") ??
            throw new Exception("gAPI.Dtos.BaseResponse was not found. " +
                "Please reference the gAPI package on the same project as gAPI.AutoPages references.");
        BaseResponse = new SharedReference(baseResponse_Symbol);
        var baseResponseT_Symbol =
            compilation.GetTypeByMetadataName("gAPI.Dtos.BaseResponseT`1") ??
            throw new Exception("gAPI.Dtos.BaseResponseT was not found. " +
                "Please reference the gAPI package on the same project as gAPI.AutoPages references.");
        BaseResponseT = new SharedReference(baseResponseT_Symbol);
        var baseListResponseT_Symbol =
            compilation.GetTypeByMetadataName("gAPI.Dtos.BaseListResponseT`1") ??
            throw new Exception("gAPI.Dtos.BaseListResponseT was not found. " +
                "Please reference the gAPI package on the same project as gAPI.AutoPages references.");
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


        var gAPI_IClientAuthenticatedHttpClient_Symbol =
            compilation.GetTypeByMetadataName("gAPI.Interfaces.IClientAuthenticatedHttpClient")
            ?? throw new Exception("gAPI.Interfaces.IClientAuthenticatedHttpClient was not found. " +
                "Please reference the gAPI package on the same project as gAPI.AutoPages references.");

        Gapi_IClientAuthenticatedHttpClient = new SharedReference(gAPI_IClientAuthenticatedHttpClient_Symbol);
        var iClientAuthenticatedHttpClient_Symbol =
            allSymbols
                .FirstOrDefault(t =>
                    t.TypeKind == TypeKind.Interface &&
                    t.AllInterfaces.Any(i =>
                        SymbolEqualityComparer.Default.Equals(i, gAPI_IClientAuthenticatedHttpClient_Symbol)));

        if (iClientAuthenticatedHttpClient_Symbol != null)
        {
            IClientAuthenticatedHttpClient = new SharedReference(iClientAuthenticatedHttpClient_Symbol);

            var clientAuthenticationService_Symbol =
                allSymbols
                    .FirstOrDefault(t =>
                        t.TypeKind == TypeKind.Class &&
                        t.AllInterfaces.Any(i =>
                            SymbolEqualityComparer.Default.Equals(i, iClientAuthenticatedHttpClient_Symbol)));

            if (clientAuthenticationService_Symbol != null)
                ClientAuthenticatedHttpClient = new SharedReference(clientAuthenticationService_Symbol);
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

    public ISharedReference[] AllComponents { get; }
    public ISharedReference Gapi_IClientAuthenticatedHttpClient { get; }
    public ISharedReference FormFileExtension { get; }
    public ISharedReference FormFile { get; }
    public ISharedReference BaseResponse { get; }
    public ISharedReference BaseResponseT { get; }
    public ISharedReference BaseListResponseT { get; }
    public ISharedReference StateChangedHandler { get; }
    public ISharedReference State { get; }
    public ISharedReference? ItemDataSource { get; }
    public ISharedReference? ListDataSource { get; }
    public ISharedReference? IClientAuthenticatedHttpClient { get; }
    public ISharedReference? ClientAuthenticatedHttpClient { get; }
    public ISharedReference? ErrorView { get; }
    public ISharedReference? LoaderView { get; }
    public ISharedReference? RedirectToHome { get; }
    public ISharedReference? RedirectToLogin { get; }
}