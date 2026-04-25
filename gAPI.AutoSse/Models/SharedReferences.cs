using gAPI.AutoSseServer.Helpers;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace gAPI.AutoSseServer.Models;

public class SharedReferences
{
    public SharedReferences(INamedTypeSymbol[] allSymbols)
    {
        AuthenticationInitializeResult = SharedReferenceFinder.Find("gAPI.Authentication.AuthenticationInitializeResult", allSymbols);
        AuthenticationHeaders = SharedReferenceFinder.Find("gAPI.Authentication.AuthenticationHeaders", allSymbols);

        FabricClient = SharedReferenceFinder.Find("gAPI.Fabric.FabricClient", allSymbols);

        SseHost = SharedReferenceFinder.Find("gAPI.Sse.SseHost", allSymbols);
        HubResult = SharedReferenceFinder.Find("gAPI.Sse.HubResult", allSymbols);
        HubResultT = new SharedReference("gAPI.Sse.HubResultT"); 
        SseEvent = SharedReferenceFinder.Find("gAPI.Sse.SseEvent", allSymbols);

        ConnectionId = SharedReferenceFinder.Find("gAPI.Ids.ConnectionId", allSymbols);
        ServiceId = SharedReferenceFinder.Find("gAPI.Ids.ServiceId", allSymbols);
        ServiceMethodId = SharedReferenceFinder.Find("gAPI.Ids.ServiceMethodId", allSymbols);
        UserId = SharedReferenceFinder.Find("gAPI.Ids.UserId", allSymbols);
        SessionId = SharedReferenceFinder.Find("gAPI.Ids.SessionId", allSymbols);

        BaseListResponseT = new SharedReference("gAPI.Dtos.BaseListResponseT"); 
        BaseResponseT = new SharedReference("gAPI.Dtos.BaseResponseT");
        BaseResponse = SharedReferenceFinder.Find("gAPI.Dtos.BaseResponse", allSymbols);
        InvokeRequestDto = SharedReferenceFinder.Find("gAPI.Dtos.InvokeRequestDto", allSymbols);
        InvokeResponseDto = SharedReferenceFinder.Find("gAPI.Dtos.InvokeResponseDto", allSymbols);
        SendRequestDto = SharedReferenceFinder.Find("gAPI.Dtos.SendRequestDto", allSymbols);
        ServerConfig = SharedReferenceFinder.Find("gAPI.Dtos.ServerConfig", allSymbols);
        SubscribeDto = SharedReferenceFinder.Find("gAPI.Dtos.SubscribeDto", allSymbols);
        UnsubscribeDto = SharedReferenceFinder.Find("gAPI.Dtos.UnsubscribeDto", allSymbols);
        ApiSendRequestDto = SharedReferenceFinder.Find("gAPI.Dtos.ApiSendRequestDto", allSymbols);
        ApiInvokeRequestDto = SharedReferenceFinder.Find("gAPI.Dtos.ApiInvokeRequestDto", allSymbols);
        ApiInvokeResponseDto = SharedReferenceFinder.Find("gAPI.Dtos.ApiInvokeResponseDto", allSymbols);
        ApiInvokeResponseDoneDto = SharedReferenceFinder.Find("gAPI.Dtos.ApiInvokeResponseDoneDto", allSymbols);

        IServerAuthenticationService = SharedReferenceFinder.Find("gAPI.Interfaces.IServerAuthenticationService", allSymbols);
        IAuthenticationSecurity = SharedReferenceFinder.Find("gAPI.Interfaces.IAuthenticationSecurity", allSymbols);
        ISseHost = SharedReferenceFinder.Find("gAPI.Interfaces.ISseHost", allSymbols);
        IUseCase = new SharedReference("gAPI.Interfaces.IUseCase");
        Mapping = new SharedReference("gAPI.Interfaces.Mapping");

        SseHostCollection = SharedReferenceFinder.Find("gAPI.Collections.SseHostCollection", allSymbols);
        WssConnectionCollection = SharedReferenceFinder.Find("gAPI.Collections.WssConnectionCollection", allSymbols);
        WssSessionCache = SharedReferenceFinder.Find("gAPI.Collections.WssSessionCache", allSymbols);
        ServerAuthenticationAccessor = SharedReferenceFinder.Find("gAPI.Wss.ServerAuthenticationAccessor", allSymbols);
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