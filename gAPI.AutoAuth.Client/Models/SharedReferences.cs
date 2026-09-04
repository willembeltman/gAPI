using gAPI.AutoAuth.Client.Helpers;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoAuth.Client.Models;

public class SharedReferences
{
    public SharedReferences(INamedTypeSymbol[] allSymbols)
    {
        SessionId = SharedReferenceFinder.Find("gAPI.Core.Ids.SessionId", allSymbols);
        UserId = SharedReferenceFinder.Find("gAPI.Core.Ids.UserId", allSymbols);
        InvokeRequestDoneDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeRequestDoneDto", allSymbols);
        InvokeRequestDoneDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeRequestDoneDto", allSymbols);
        ClientConfig = SharedReferenceFinder.Find("gAPI.Core.Client.Config.ClientConfig", allSymbols);
        IClientLoggerFactory = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IClientLoggerFactory", allSymbols);
        IClientAuthenticatedHttpClient = SharedReferenceFinder.Find("gAPI.Core.Client.Interfaces.IClientAuthenticatedHttpClient", allSymbols);
        IWssClientConnection = SharedReferenceFinder.Find("gAPI.Core.Client.Interfaces.IWssClientConnection", allSymbols);
        IUriNavigationManager = SharedReferenceFinder.Find("gAPI.Core.Client.Interfaces.IUriNavigationManager", allSymbols);
        DefaultNavigationManager = SharedReferenceFinder.Find("gAPI.Core.Client.Navigation.DefaultNavigationManager", allSymbols);
        StaticNavigationManager = SharedReferenceFinder.Find("gAPI.Core.Client.Navigation.StaticNavigationManager", allSymbols);
        WithCookiesHandler = SharedReferenceFinder.Find("gAPI.Core.Client.Razor.WithCookiesHandler", allSymbols);
        StateChangedHandler = SharedReferenceFinder.Find("gAPI.Core.Delegates.StateChangedHandler", allSymbols);
        IStateParserT = new SharedReference("gAPI.Core.Interfaces.IStateParser");

        AuthStateDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.AuthStateDto", allSymbols);
        StateDto = SharedReferenceFinder.TryFindByBaseType(AuthStateDto, allSymbols);
        IClientAuthenticatedHttpClientImplementation = SharedReferenceFinder.TryFindByInterface(IClientAuthenticatedHttpClient, allSymbols);

    }

    public SharedReference SessionId { get; }
    public SharedReference UserId { get; }
    public SharedReference InvokeRequestDoneDto { get; }
    public SharedReference IClientLoggerFactory { get; }
    public SharedReference IClientAuthenticatedHttpClient { get; }
    public SharedReference IWssClientConnection { get; }
    public SharedReference ClientConfig { get; }
    public SharedReference IUriNavigationManager { get; }
    public SharedReference DefaultNavigationManager { get; }
    public SharedReference StaticNavigationManager { get; }
    public SharedReference WithCookiesHandler { get; }
    public SharedReference IStateParserT { get; }
    public SharedReference AuthStateDto { get; }
    public SharedReference? StateDto { get; }
    public SharedReference StateChangedHandler { get; }
    public SharedReference? IClientAuthenticatedHttpClientImplementation { get; }
}