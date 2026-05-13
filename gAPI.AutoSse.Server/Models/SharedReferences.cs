using gAPI.AutoSseServer.Helpers;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoSseServer.Models;

public class SharedReferences
{
    public SharedReferences(INamedTypeSymbol[] allSymbols)
    {
        AuthenticationInitializeResult = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.AuthenticationInitializeResult", allSymbols);
        AuthenticationHeaders = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.AuthenticationHeaders", allSymbols);

        FabricClient = SharedReferenceFinder.Find("gAPI.Core.Server.Fabric.FabricClient", allSymbols);

        SseHost = SharedReferenceFinder.Find("gAPI.Core.Sse.SseHost", allSymbols);
        HubResult = SharedReferenceFinder.Find("gAPI.Core.Sse.HubResult", allSymbols);
        HubResultT = new SharedReference("gAPI.Core.Sse.HubResultT");
        SseEvent = SharedReferenceFinder.Find("gAPI.Core.Sse.SseEvent", allSymbols);

        ConnectionId = SharedReferenceFinder.Find("gAPI.Core.Ids.ConnectionId", allSymbols);
        ServiceId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceId", allSymbols);
        ServiceMethodId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceMethodId", allSymbols);
        UserId = SharedReferenceFinder.Find("gAPI.Core.Ids.UserId", allSymbols);
        SessionId = SharedReferenceFinder.Find("gAPI.Core.Ids.SessionId", allSymbols);

        BaseListResponseT = new SharedReference("gAPI.Core.Dtos.BaseListResponseT");
        BaseResponseT = new SharedReference("gAPI.Core.Dtos.BaseResponseT");
        BaseResponse = SharedReferenceFinder.Find("gAPI.Core.Dtos.BaseResponse", allSymbols);
        InvokeRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeRequestDto", allSymbols);
        InvokeResponseDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeResponseDto", allSymbols);
        SendRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.SendRequestDto", allSymbols);
        ServerConfig = SharedReferenceFinder.Find("gAPI.Core.Dtos.ServerConfig", allSymbols);
        SubscribeDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.SubscribeDto", allSymbols);
        UnsubscribeDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.UnsubscribeDto", allSymbols);
        ApiSendRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.ApiSendRequestDto", allSymbols);
        ApiInvokeRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.ApiInvokeRequestDto", allSymbols);
        ApiInvokeResponseDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.ApiInvokeResponseDto", allSymbols);
        ApiInvokeResponseDoneDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.ApiInvokeResponseDoneDto", allSymbols);

        IServerAuthenticationService = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IServerAuthenticationService", allSymbols);
        IAuthenticationSecurity = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IAuthenticationSecurity", allSymbols);
        ISseHost = SharedReferenceFinder.Find("gAPI.Core.Interfaces.ISseHost", allSymbols);
        IUseCase = new SharedReference("gAPI.Core.Interfaces.IUseCase");
        Mapping = new SharedReference("gAPI.Core.Interfaces.Mapping");

        SseHostCollection = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.SseHostCollection", allSymbols);
        WssConnectionCollection = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.WssConnectionCollection", allSymbols);
        WssSessionCache = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.WssSessionCache", allSymbols);
        ServerAuthenticationAccessor = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.ServerAuthenticationAccessor", allSymbols);
    }

    public SharedReference FabricClient { get; }
    public SharedReference SseHostCollection { get; }
    public SharedReference SseHost { get; }
    public SharedReference ServiceId { get; }
    public SharedReference ServiceMethodId { get; }
    public SharedReference UserId { get; }
    public SharedReference SessionId { get; }
    public SharedReference BaseListResponseT { get; }
    public SharedReference BaseResponse { get; }
    public SharedReference BaseResponseT { get; }
    public SharedReference InvokeRequestDto { get; }
    public SharedReference InvokeResponseDto { get; }
    public SharedReference IServerAuthenticationService { get; }
    public SharedReference IAuthenticationSecurity { get; }
    public SharedReference ISseHost { get; }
    public SharedReference IUseCase { get; }
    public SharedReference Mapping { get; }
    public SharedReference HubResult { get; }
    public SharedReference HubResultT { get; }
    public SharedReference SseEvent { get; }
    public SharedReference ConnectionId { get; }
    public SharedReference AuthenticationInitializeResult { get; }
    public SharedReference AuthenticationHeaders { get; }
    public SharedReference SendRequestDto { get; }
    public SharedReference ServerConfig { get; }
    public SharedReference SubscribeDto { get; }
    public SharedReference UnsubscribeDto { get; }
    public SharedReference ApiSendRequestDto { get; }
    public SharedReference WssConnectionCollection { get; }
    public SharedReference ApiInvokeRequestDto { get; }
    public SharedReference ApiInvokeResponseDto { get; }
    public SharedReference WssSessionCache { get; }
    public SharedReference ServerAuthenticationAccessor { get; }
    public SharedReference ApiInvokeResponseDoneDto { get; }
}