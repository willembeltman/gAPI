using gAPI.AutoWssClient.Helpers;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoWssClient.Models;

public class SharedReferences
{
    public SharedReferences(INamedTypeSymbol[] allSymbols)
    {
        ServiceId = SharedReferenceFinder.Find("gAPI.Ids.ServiceId", allSymbols);
        RequestId = SharedReferenceFinder.Find("gAPI.Ids.RequestId", allSymbols);
        BaseResponseT = new SharedReference("gAPI.Dtos.BaseResponseT"); 
        BaseResponse = SharedReferenceFinder.Find("gAPI.Dtos.BaseResponse", allSymbols);
        InvokeRequestDto = SharedReferenceFinder.Find("gAPI.Dtos.InvokeRequestDto", allSymbols);
        InvokeResponseDto = SharedReferenceFinder.Find("gAPI.Dtos.InvokeResponseDto", allSymbols);
        SendRequestDto = SharedReferenceFinder.Find("gAPI.Dtos.SendRequestDto", allSymbols);
        SubscribeDto = SharedReferenceFinder.Find("gAPI.Dtos.SubscribeDto", allSymbols);
        UnsubscribeDto = SharedReferenceFinder.Find("gAPI.Dtos.UnsubscribeDto", allSymbols);
        ApiSendRequestDto = SharedReferenceFinder.Find("gAPI.Dtos.ApiSendRequestDto", allSymbols);
        ApiInvokeRequestDto = SharedReferenceFinder.Find("gAPI.Dtos.ApiInvokeRequestDto", allSymbols);
        ApiInvokeResponseDto = SharedReferenceFinder.Find("gAPI.Dtos.ApiInvokeResponseDto", allSymbols);
        ApiInvokeResponseDoneDto = SharedReferenceFinder.Find("gAPI.Dtos.ApiInvokeResponseDoneDto", allSymbols);
        InvokeResponseDoneDto = SharedReferenceFinder.Find("gAPI.Dtos.InvokeResponseDoneDto", allSymbols);
        FrontendConfig = SharedReferenceFinder.Find("gAPI.Dtos.FrontendConfig", allSymbols);
        IWssLoggerFactory = SharedReferenceFinder.Find("gAPI.Interfaces.IWssLoggerFactory", allSymbols);
        IClientAuthenticatedHttpClient = SharedReferenceFinder.Find("gAPI.Interfaces.IClientAuthenticatedHttpClient", allSymbols);
        WssClientConnection = SharedReferenceFinder.Find("gAPI.Wss.WssClientConnection", allSymbols);
        IWssClientConnection = SharedReferenceFinder.Find("gAPI.Interfaces.IWssClientConnection", allSymbols);
        AuthClient_FormFile = SharedReferenceFinder.TryFindByAttribute("gAPI.Attributes.IsFormFileAttribute", allSymbols);
        AuthClient_ToFormFileExtension = SharedReferenceFinder.TryFindByAttribute("gAPI.Attributes.IsFormFileExtensionAttribute", allSymbols);
    }

    public SharedReference ServiceId { get; }
    public SharedReference BaseResponse { get; }
    public SharedReference BaseResponseT { get; }
    public SharedReference InvokeRequestDto { get; }
    public SharedReference InvokeResponseDto { get; }
    public SharedReference SendRequestDto { get; }
    public SharedReference SubscribeDto { get; }
    public SharedReference UnsubscribeDto { get; }
    public SharedReference ApiSendRequestDto { get; }
    public SharedReference ApiInvokeRequestDto { get; }
    public SharedReference ApiInvokeResponseDto { get; }
    public SharedReference WssClientConnection { get; }
    public SharedReference IWssLoggerFactory { get; }
    public SharedReference IClientAuthenticatedHttpClient { get; }
    public SharedReference RequestId { get; }
    public SharedReference IWssClientConnection { get; }
    public SharedReference? AuthClient_FormFile { get; }
    public SharedReference? AuthClient_ToFormFileExtension { get; }
    public SharedReference ApiInvokeResponseDoneDto { get; }
    public SharedReference InvokeResponseDoneDto { get; }
    public SharedReference FrontendConfig { get;  }
}