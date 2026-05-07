using gAPI.AutoSseClient.Generators;
using gAPI.AutoSseClient.Helpers;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoSseClient.Models;

public class SharedReferences
{
    public SharedReferences(INamedTypeSymbol[] allSymbols)
    {
        //AuthenticationInitializeResult = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.AuthenticationInitializeResult", allSymbols);
        //AuthenticationHeaders = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.AuthenticationHeaders", allSymbols);

        //FabricClient = SharedReferenceFinder.Find("gAPI.Core.Server.Fabric.FabricClient", allSymbols);

        //SseHost = SharedReferenceFinder.Find("gAPI.Core.Sse.SseHost", allSymbols);
        //WssService = SharedReferenceFinder.Find("gAPI.Core.Sse.WssService", allSymbols);
        //HubResult = SharedReferenceFinder.Find("gAPI.Core.Sse.HubResult", allSymbols);
        //HubResultT = new SharedReference("gAPI.Core.Sse.HubResultT"); //Find("gAPI.Core.Sse.HubResultT", allSymbols);
        //SseEvent = SharedReferenceFinder.Find("gAPI.Core.Sse.SseEvent", allSymbols);
        SendRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.SendRequestDto", allSymbols);

        //ConnectionId = SharedReferenceFinder.Find("gAPI.Core.Ids.ConnectionId", allSymbols);
        ServiceId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceId", allSymbols);
        //ServiceMethodId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceMethodId", allSymbols);
        //UserId = SharedReferenceFinder.Find("gAPI.Core.Ids.UserId", allSymbols);
        //SessionId = SharedReferenceFinder.Find("gAPI.Core.Ids.SessionId", allSymbols);
        //RequestId = SharedReferenceFinder.Find("gAPI.Core.Ids.RequestId", allSymbols);
        SseHostId = SharedReferenceFinder.Find("gAPI.Core.Ids.SseHostId", allSymbols);
        SseManagerId = SharedReferenceFinder.Find("gAPI.Core.Ids.SseManagerId", allSymbols);

        //BaseListResponseT = new SharedReference("gAPI.Core.Dtos.BaseListResponseT"); //Find("gAPI.Core.Dtos.BaseListResponseT", allSymbols);
        //BaseResponseT = new SharedReference("gAPI.Core.Dtos.BaseResponseT"); //Find("gAPI.Core.Dtos.BaseResponseT", allSymbols);
        //BaseResponse = SharedReferenceFinder.Find("gAPI.Core.Dtos.BaseResponse", allSymbols);
        //InvokeRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeRequestDto", allSymbols);
        //InvokeResponseDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeResponseDto", allSymbols);
        SendRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.SendRequestDto", allSymbols);
        //ServerConfig = SharedReferenceFinder.Find("gAPI.Core.Server.Dtos.ServerConfig", allSymbols);
        //SubscribeDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.SubscribeDto", allSymbols);
        //UnsubscribeDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.UnsubscribeDto", allSymbols);
        //ApiSendRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.ApiSendRequestDto", allSymbols);
        //ApiInvokeRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.ApiInvokeRequestDto", allSymbols);
        //ApiInvokeResponseDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.ApiInvokeResponseDto", allSymbols);
        //ApiInvokeResponseDoneDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.ApiInvokeResponseDoneDto", allSymbols);
        //InvokeResponseDoneDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeResponseDoneDto", allSymbols);
        //FrontendConfig = SharedReferenceFinder.Find("gAPI.Core.Dtos.FrontendConfig", allSymbols);

        //IServerAuthenticationService = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IServerAuthenticationService", allSymbols);
        //IAuthenticationSecurity = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IAuthenticationSecurity", allSymbols);
        //ISseHost = SharedReferenceFinder.Find("gAPI.Core.Interfaces.ISseHost", allSymbols);
        //IUseCase = new SharedReference("gAPI.Core.Interfaces.IUseCase"); //Find("gAPI.Core.Interfaces.IUseCase", allSymbols);
        //Mapping = new SharedReference("gAPI.Core.Interfaces.Mapping"); //Find("gAPI.Core.Interfaces.Mapping", allSymbols);
        //IWssLoggerFactory = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IWssLoggerFactory", allSymbols);
        IClientAuthenticatedHttpClient = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IClientAuthenticatedHttpClient", allSymbols);
        IClientConnection = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IClientConnection", allSymbols);

        //SseHostCollection = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.SseHostCollection", allSymbols);
        //WssConnectionCollection = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.WssConnectionCollection", allSymbols);
        SseManagerCollection = SharedReferenceFinder.Find("gAPI.Core.Collections.SseManagerCollection", allSymbols);

        //WssClientConnection = SharedReferenceFinder.Find("gAPI.Core.Wss.WssClientConnection", allSymbols);
        //IWssClientConnection = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IWssClientConnection", allSymbols);
        SseClient = SharedReferenceFinder.Find("gAPI.Core.Sse.SseClient", allSymbols);
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
    public SharedReference IClientConnection { get; }
    public SharedReference SseHostId { get; }
    public SharedReference SseManagerId { get; }
    public SharedReference SseClient { get; }
}