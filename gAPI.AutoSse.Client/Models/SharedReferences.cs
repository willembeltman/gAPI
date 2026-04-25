using gAPI.AutoSseClient.Helpers;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoSseClient.Models;

public class SharedReferences
{
    public SharedReferences(INamedTypeSymbol[] allSymbols)
    {
        //AuthenticationInitializeResult = SharedReferenceFinder.Find("gAPI.Authentication.AuthenticationInitializeResult", allSymbols);
        //AuthenticationHeaders = SharedReferenceFinder.Find("gAPI.Authentication.AuthenticationHeaders", allSymbols);

        //FabricClient = SharedReferenceFinder.Find("gAPI.Fabric.FabricClient", allSymbols);

        //SseHost = SharedReferenceFinder.Find("gAPI.Sse.SseHost", allSymbols);
        //WssService = SharedReferenceFinder.Find("gAPI.Sse.WssService", allSymbols);
        //HubResult = SharedReferenceFinder.Find("gAPI.Sse.HubResult", allSymbols);
        //HubResultT = new SharedReference("gAPI.Sse.HubResultT"); //Find("gAPI.Sse.HubResultT", allSymbols);
        //SseEvent = SharedReferenceFinder.Find("gAPI.Sse.SseEvent", allSymbols);
        SendRequestDto = SharedReferenceFinder.Find("gAPI.Dtos.SendRequestDto", allSymbols);

        //ConnectionId = SharedReferenceFinder.Find("gAPI.Ids.ConnectionId", allSymbols);
        ServiceId = SharedReferenceFinder.Find("gAPI.Ids.ServiceId", allSymbols);
        //ServiceMethodId = SharedReferenceFinder.Find("gAPI.Ids.ServiceMethodId", allSymbols);
        //UserId = SharedReferenceFinder.Find("gAPI.Ids.UserId", allSymbols);
        //SessionId = SharedReferenceFinder.Find("gAPI.Ids.SessionId", allSymbols);
        //RequestId = SharedReferenceFinder.Find("gAPI.Ids.RequestId", allSymbols);
        SseHostId = SharedReferenceFinder.Find("gAPI.Ids.SseHostId", allSymbols);
        SseManagerId = SharedReferenceFinder.Find("gAPI.Ids.SseManagerId", allSymbols);

        //BaseListResponseT = new SharedReference("gAPI.Dtos.BaseListResponseT"); //Find("gAPI.Dtos.BaseListResponseT", allSymbols);
        //BaseResponseT = new SharedReference("gAPI.Dtos.BaseResponseT"); //Find("gAPI.Dtos.BaseResponseT", allSymbols);
        //BaseResponse = SharedReferenceFinder.Find("gAPI.Dtos.BaseResponse", allSymbols);
        //InvokeRequestDto = SharedReferenceFinder.Find("gAPI.Dtos.InvokeRequestDto", allSymbols);
        //InvokeResponseDto = SharedReferenceFinder.Find("gAPI.Dtos.InvokeResponseDto", allSymbols);
        SendRequestDto = SharedReferenceFinder.Find("gAPI.Dtos.SendRequestDto", allSymbols);
        //ServerConfig = SharedReferenceFinder.Find("gAPI.Dtos.ServerConfig", allSymbols);
        //SubscribeDto = SharedReferenceFinder.Find("gAPI.Dtos.SubscribeDto", allSymbols);
        //UnsubscribeDto = SharedReferenceFinder.Find("gAPI.Dtos.UnsubscribeDto", allSymbols);
        //ApiSendRequestDto = SharedReferenceFinder.Find("gAPI.Dtos.ApiSendRequestDto", allSymbols);
        //ApiInvokeRequestDto = SharedReferenceFinder.Find("gAPI.Dtos.ApiInvokeRequestDto", allSymbols);
        //ApiInvokeResponseDto = SharedReferenceFinder.Find("gAPI.Dtos.ApiInvokeResponseDto", allSymbols);
        //ApiInvokeResponseDoneDto = SharedReferenceFinder.Find("gAPI.Dtos.ApiInvokeResponseDoneDto", allSymbols);
        //InvokeResponseDoneDto = SharedReferenceFinder.Find("gAPI.Dtos.InvokeResponseDoneDto", allSymbols);
        //FrontendConfig = SharedReferenceFinder.Find("gAPI.Dtos.FrontendConfig", allSymbols);

        //IServerAuthenticationService = SharedReferenceFinder.Find("gAPI.Interfaces.IServerAuthenticationService", allSymbols);
        //IAuthenticationSecurity = SharedReferenceFinder.Find("gAPI.Interfaces.IAuthenticationSecurity", allSymbols);
        //ISseHost = SharedReferenceFinder.Find("gAPI.Interfaces.ISseHost", allSymbols);
        //IUseCase = new SharedReference("gAPI.Interfaces.IUseCase"); //Find("gAPI.Interfaces.IUseCase", allSymbols);
        //Mapping = new SharedReference("gAPI.Interfaces.Mapping"); //Find("gAPI.Interfaces.Mapping", allSymbols);
        //IWssLoggerFactory = SharedReferenceFinder.Find("gAPI.Interfaces.IWssLoggerFactory", allSymbols);
        IClientAuthenticatedHttpClient = SharedReferenceFinder.Find("gAPI.Interfaces.IClientAuthenticatedHttpClient", allSymbols);
        ISseClientConnection = SharedReferenceFinder.Find("gAPI.Interfaces.ISseClientConnection", allSymbols);

        //SseHostCollection = SharedReferenceFinder.Find("gAPI.Collections.SseHostCollection", allSymbols);
        //WssConnectionCollection = SharedReferenceFinder.Find("gAPI.Collections.WssConnectionCollection", allSymbols);
        SseManagerCollection = SharedReferenceFinder.Find("gAPI.Collections.SseManagerCollection", allSymbols);

        //WssClientConnection = SharedReferenceFinder.Find("gAPI.Wss.WssClientConnection", allSymbols);
        //IWssClientConnection = SharedReferenceFinder.Find("gAPI.Interfaces.IWssClientConnection", allSymbols);
    }

    //public SharedReference FabricClient { get; }
    //public SharedReference SseHostCollection { get; }
    //public SharedReference SseHost { get; }
    public SharedReference ServiceId { get; }
    //public SharedReference ServiceMethodId { get; }
    //public SharedReference UserId { get; }
    //public SharedReference SessionId { get; }
    //public SharedReference BaseListResponseT { get; }
    //public SharedReference BaseResponse { get; }
    //public SharedReference BaseResponseT { get; }
    //public SharedReference InvokeRequestDto { get; }
    //public SharedReference InvokeResponseDto { get; }
    //public SharedReference IServerAuthenticationService { get; }
    //public SharedReference IAuthenticationSecurity { get; }
    //public SharedReference ISseHost { get; }
    //public SharedReference IUseCase { get; }
    //public SharedReference Mapping { get; }
    //public SharedReference HubResult { get; }
    //public SharedReference HubResultT { get; }
    //public SharedReference SseEvent { get; }
    //public SharedReference ConnectionId { get; }
    //public SharedReference AuthenticationInitializeResult { get; }
    //public SharedReference AuthenticationHeaders { get; }
    public SharedReference SendRequestDto { get; }
    //public SharedReference ServerConfig { get; }
    //public SharedReference SubscribeDto { get; }
    //public SharedReference UnsubscribeDto { get; }
    //public SharedReference ApiSendRequestDto { get; }
    //public SharedReference WssConnectionCollection { get; }
    //public SharedReference ApiInvokeRequestDto { get; }
    //public SharedReference ApiInvokeResponseDto { get; }
    //public SharedReference WssClientConnection { get; }
    //public SharedReference IWssLoggerFactory { get; }
    public SharedReference IClientAuthenticatedHttpClient { get; }
    //public SharedReference RequestId { get; }
    //public SharedReference IWssClientConnection { get; }
    //public SharedReference ApiInvokeResponseDoneDto { get; }
    //public SharedReference InvokeResponseDoneDto { get; }
    //public SharedReference FrontendConfig { get;  }
    public SharedReference SseManagerCollection { get;}
    public SharedReference ISseClientConnection { get; }
    public SharedReference SseHostId { get; }
    public SharedReference SseManagerId { get; }
}