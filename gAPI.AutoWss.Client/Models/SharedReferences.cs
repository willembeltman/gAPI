using gAPI.AutoWssClient.Helpers;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoWssClient.Models;

public class SharedReferences
{
    public SharedReferences(INamedTypeSymbol[] allSymbols)
    {
        ServiceId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceId", allSymbols);
        RequestId = SharedReferenceFinder.Find("gAPI.Core.Ids.RequestId", allSymbols);
        BaseResponseT = new SharedReference("gAPI.Core.Dtos.BaseResponseT");
        BaseResponse = SharedReferenceFinder.Find("gAPI.Core.Dtos.BaseResponse", allSymbols);
        InvokeRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeRequestDto", allSymbols);
        InvokeResponseDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeResponseDto", allSymbols);
        SendRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.SendRequestDto", allSymbols);
        SubscribeDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.SubscribeDto", allSymbols);
        UnsubscribeDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.UnsubscribeDto", allSymbols);
        ApiSendRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.ApiSendRequestDto", allSymbols);
        ApiInvokeRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.ApiInvokeRequestDto", allSymbols);
        ApiInvokeResponseDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.ApiInvokeResponseDto", allSymbols);
        ApiInvokeResponseDoneDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.ApiInvokeResponseDoneDto", allSymbols);
        InvokeResponseDoneDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeResponseDoneDto", allSymbols);
        FrontendConfig = SharedReferenceFinder.Find("gAPI.Core.Dtos.FrontendConfig", allSymbols);
        IWssLoggerFactory = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IWssLoggerFactory", allSymbols);
        IClientAuthenticatedHttpClient = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IClientAuthenticatedHttpClient", allSymbols);
        WssClientConnection = SharedReferenceFinder.Find("gAPI.Core.Wss.WssClientConnection", allSymbols);
        IWssClientConnection = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IWssClientConnection", allSymbols);
        AuthClient_FormFile = SharedReferenceFinder.TryFindByAttribute("gAPI.Core.Attributes.IsFormFileAttribute", allSymbols);
        AuthClient_ToFormFileExtension = SharedReferenceFinder.TryFindByAttribute("gAPI.Core.Attributes.IsFormFileExtensionAttribute", allSymbols);
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
    public SharedReference FrontendConfig { get; }
}