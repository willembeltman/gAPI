using gAPI.AutoAuth.Client.Helpers;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoAuth.Client.Models;

public class SharedReferences
{
    public SharedReferences(INamedTypeSymbol[] allSymbols)
    {
        ServiceId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceId", allSymbols);
        RequestId = SharedReferenceFinder.Find("gAPI.Core.Ids.RequestId", allSymbols);
        SessionId = SharedReferenceFinder.Find("gAPI.Core.Ids.SessionId", allSymbols);
        UserId = SharedReferenceFinder.Find("gAPI.Core.Ids.UserId", allSymbols);
        BaseResponseT = new SharedReference("gAPI.Core.Dtos.BaseResponseT");
        BaseResponse = SharedReferenceFinder.Find("gAPI.Core.Dtos.BaseResponse", allSymbols);
        SubscribeDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.SubscribeDto", allSymbols);
        UnsubscribeDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.UnsubscribeDto", allSymbols);
        SendRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.SendRequestDto", allSymbols);
        InvokeRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeRequestDto", allSymbols);
        InvokeResponseDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeResponseDto", allSymbols);
        InvokeResponseDoneDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeResponseDoneDto", allSymbols);
        InvokeResponseDoneDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeResponseDoneDto", allSymbols);
        ClientConfig = SharedReferenceFinder.Find("gAPI.Core.Client.Config.ClientConfig", allSymbols);
        IClientLoggerFactory = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IClientLoggerFactory", allSymbols);
        IClientAuthenticatedHttpClient = SharedReferenceFinder.Find("gAPI.Core.Client.Interfaces.IClientAuthenticatedHttpClient", allSymbols);
        WssClientConnection = SharedReferenceFinder.Find("gAPI.Core.Client.Wss.WssClientConnection", allSymbols);
        IWssClientConnection = SharedReferenceFinder.Find("gAPI.Core.Client.Interfaces.IWssClientConnection", allSymbols);
        AuthClient_FormFile = SharedReferenceFinder.TryFindByAttribute("gAPI.Core.Attributes.IsFormFileAttribute", allSymbols);
        AuthClient_ToFormFileExtension = SharedReferenceFinder.TryFindByAttribute("gAPI.Core.Attributes.IsFormFileExtensionAttribute", allSymbols);
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

    public SharedReference ServiceId { get; }
    public SharedReference RequestId { get; }
    public SharedReference SessionId { get; }
    public SharedReference UserId { get; }
    public SharedReference BaseResponse { get; }
    public SharedReference BaseResponseT { get; }
    public SharedReference SubscribeDto { get; }
    public SharedReference UnsubscribeDto { get; }
    public SharedReference SendRequestDto { get; }
    public SharedReference InvokeRequestDto { get; }
    public SharedReference InvokeResponseDto { get; }
    public SharedReference InvokeResponseDoneDto { get; }
    public SharedReference WssClientConnection { get; }
    public SharedReference IClientLoggerFactory { get; }
    public SharedReference IClientAuthenticatedHttpClient { get; }
    public SharedReference IWssClientConnection { get; }
    public SharedReference? AuthClient_FormFile { get; }
    public SharedReference? AuthClient_ToFormFileExtension { get; }
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