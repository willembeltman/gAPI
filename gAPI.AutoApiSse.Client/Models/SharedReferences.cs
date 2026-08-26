using gAPI.AutoApiSse.Client.Helpers;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoApiSse.Client.Models;

public class SharedReferences
{
    public SharedReferences(INamedTypeSymbol[] allSymbols)
    {
        ServiceId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceId", allSymbols);
        RequestId = SharedReferenceFinder.Find("gAPI.Core.Ids.RequestId", allSymbols);
        SessionId = SharedReferenceFinder.Find("gAPI.Core.Ids.SessionId", allSymbols);
        UserId = SharedReferenceFinder.Find("gAPI.Core.Ids.UserId", allSymbols);

        SendRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.SendRequestDto", allSymbols);
        SseManagerId = SharedReferenceFinder.Find("gAPI.Core.Ids.SseManagerId", allSymbols);
        SendRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.SendRequestDto", allSymbols);
        IClientAuthenticatedHttpClient = SharedReferenceFinder.Find("gAPI.Core.Client.Interfaces.IClientAuthenticatedHttpClient", allSymbols);
        ISseClientConnection = SharedReferenceFinder.Find("gAPI.Core.Client.Interfaces.ISseClientConnection", allSymbols);

        SseManagerCollection = SharedReferenceFinder.Find("gAPI.Core.Client.Collections.SseManagerCollection", allSymbols);
        SseClient = SharedReferenceFinder.Find("gAPI.Core.Client.Sse.SseClient", allSymbols);

        AuthClient_FormFile = SharedReferenceFinder.TryFindByAttribute("gAPI.Core.Attributes.IsFormFileAttribute", allSymbols);
        AuthClient_ToFormFileExtension = SharedReferenceFinder.TryFindByAttribute("gAPI.Core.Attributes.IsFormFileExtensionAttribute", allSymbols);
        
        IUriNavigationManager = SharedReferenceFinder.Find("gAPI.Core.Client.Interfaces.IUriNavigationManager", allSymbols);
        DefaultNavigationManager = SharedReferenceFinder.Find("gAPI.Core.Client.Navigation.DefaultNavigationManager", allSymbols);
        StaticNavigationManager = SharedReferenceFinder.Find("gAPI.Core.Client.Navigation.StaticNavigationManager", allSymbols);
        WithCookiesHandler = SharedReferenceFinder.Find("gAPI.Core.Client.Razor.WithCookiesHandler", allSymbols);
        StateChangedHandler = SharedReferenceFinder.Find("gAPI.Core.Delegates.StateChangedHandler", allSymbols);



        AuthStateDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.AuthStateDto", allSymbols);
        StateDto = SharedReferenceFinder.TryFindByBaseType(AuthStateDto, allSymbols);
        IClientAuthenticatedHttpClientImplementation = SharedReferenceFinder.TryFindByInterface(IClientAuthenticatedHttpClient, allSymbols);

    }
    public SharedReference ServiceId { get; }
    public SharedReference RequestId { get; }
    public SharedReference SessionId { get; }
    public SharedReference UserId { get; }
    public SharedReference SendRequestDto { get; }
    public SharedReference IClientAuthenticatedHttpClient { get; }
    public SharedReference SseManagerCollection { get; }
    public SharedReference ISseClientConnection { get; }
    public SharedReference SseManagerId { get; }
    public SharedReference SseClient { get; }
    public SharedReference? AuthClient_FormFile { get; }
    public SharedReference? AuthClient_ToFormFileExtension { get; }
    public SharedReference IUriNavigationManager { get; }
    public SharedReference DefaultNavigationManager { get; }
    public SharedReference StaticNavigationManager { get; }
    public SharedReference WithCookiesHandler { get; }
    public SharedReference StateChangedHandler { get; }
    public SharedReference AuthStateDto { get; }
    public SharedReference? StateDto { get; }
    public SharedReference? IClientAuthenticatedHttpClientImplementation { get; }
}