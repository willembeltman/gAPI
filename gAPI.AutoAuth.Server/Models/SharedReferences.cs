using gAPI.AutoAuth.Server.Helpers;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace gAPI.AutoAuth.Server.Models;

public class SharedReferences
{
    public SharedReferences(INamedTypeSymbol[] allSymbols)
    {
        AuthenticationInitializeResult = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.AuthenticationInitializeResult", allSymbols);
        AuthenticationHeaders = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.AuthenticationHeaders", allSymbols);

        FabricClient = SharedReferenceFinder.Find("gAPI.Core.Server.Fabric.FabricClient", allSymbols);

        Ip = new SharedReference("gAPI.Core.Server.Entities.Ip");
        UserToken = new SharedReference("gAPI.Core.Server.Entities.UserToken");
        AuthStateUserDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.AuthStateUserDto", allSymbols);

        //SseHost = SharedReferenceFinder.Find("gAPI.Core.Server.Sse.SseHost", allSymbols);
        //WssService = SharedReferenceFinder.Find("gAPI.Core.Sse.WssService", allSymbols);
        HubResult = SharedReferenceFinder.Find("gAPI.Core.Sse.HubResult", allSymbols);
        HubResultT = new SharedReference("gAPI.Core.Sse.HubResultT");
        SseEvent = SharedReferenceFinder.Find("gAPI.Core.Sse.SseEvent", allSymbols);

        ConnectionId = SharedReferenceFinder.Find("gAPI.Core.Ids.ConnectionId", allSymbols);
        ServiceId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceId", allSymbols);
        ServiceMethodId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceMethodId", allSymbols);
        UserId = SharedReferenceFinder.Find("gAPI.Core.Ids.UserId", allSymbols);
        SessionId = SharedReferenceFinder.Find("gAPI.Core.Ids.SessionId", allSymbols);

        BaseListResponseT = new SharedReference("gAPI.Core.Dtos.BaseListResponseT"); //Find("gAPI.Core.Dtos.BaseListResponseT", allSymbols);
        BaseResponseT = new SharedReference("gAPI.Core.Dtos.BaseResponseT"); //Find("gAPI.Core.Dtos.BaseResponseT", allSymbols);
        BaseResponse = SharedReferenceFinder.Find("gAPI.Core.Dtos.BaseResponse", allSymbols);
        BaseResponseErrorEnum = SharedReferenceFinder.Find("gAPI.Core.Enums.BaseResponseErrorEnum", allSymbols);
        StringHelper = SharedReferenceFinder.Find("gAPI.Core.Extensions.StringHelper", allSymbols);
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
        AuthenticationHandler = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.AuthenticationHandler", allSymbols);
        EmptyServerAuthenticationServiceT = new("gAPI.Core.Server.Authentication.EmptyServerAuthenticationService");
        
        ISseHost = SharedReferenceFinder.Find("gAPI.Core.Interfaces.ISseHost", allSymbols);
        IUseCase = new("gAPI.Core.Interfaces.IUseCase");
        Mapping = new("gAPI.Core.Interfaces.Mapping");
        AccountServiceT = new("gAPI.Core.Server.Authentication.AccountService");
        AuthenticationSecurityT = new("gAPI.Core.Server.Authentication.AuthenticationSecurity");
        IAuthenticationStateFactoryT = new("gAPI.Core.Server.Interfaces.IAuthenticationStateFactory");
        AuthenticationStateFactoryT = new("gAPI.Core.Server.Authentication.AuthenticationStateFactory");
        IUserTokenFactoryT = new("gAPI.Core.Server.Authentication.IUserTokenFactory");
        UserTokenFactoryT = new("gAPI.Core.Server.Authentication.UserTokenFactory");

        SseHostCollection = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.SseHostCollection", allSymbols);
        WssServerConnectionCollection = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.WssServerConnectionCollection", allSymbols);
        WssSessionCache = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.WssSessionCache", allSymbols);
        ServerAuthenticationAccessor = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.ServerAuthenticationAccessor", allSymbols);
        AuthenticationState = new("gAPI.Core.Server.Authentication.AuthenticationState");

        IAccountService = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IAccountService", allSymbols);
        IAuthenticationCheckT = new("gAPI.Core.Server.Interfaces.IAuthenticationCheck");
        AuthenticationServiceT = new("gAPI.Core.Server.Authentication.AuthenticationService");
        IAuthenticationServiceT = new("gAPI.Core.Server.Interfaces.IAuthenticationService");

        AuthUser = SharedReferenceFinder.Find("gAPI.Core.Server.Entities.AuthUser", allSymbols);
        User = SharedReferenceFinder.TryFindByBaseTypeNameStart("gAPI.Core.Server.Entities.AuthUser", allSymbols);

        AuthStateDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.AuthStateDto", allSymbols);
        StateDto = SharedReferenceFinder.TryFindByBaseTypeNameStart("gAPI.Core.Dtos.AuthStateDto", allSymbols);
        IStateMappingT = new("gAPI.Core.Server.Interfaces.IStateMapping");

        IStateParserT = new("gAPI.Core.Interfaces.IStateParser");
        IServerAuthenticationServiceImplementation = SharedReferenceFinder.TryFindByInterface(IServerAuthenticationService, allSymbols);

        AuthenticationDbContextT = SharedReferenceFinder.Find("gAPI.Core.Server.Entities.AuthenticationDbContext<TUser>", allSymbols);
        CustomDbContext = SharedReferenceFinder.TryFindByBaseTypeNameStart("gAPI.Core.Server.Entities.AuthenticationDbContext", allSymbols);

        AuthenticationStateMappingT = new("gAPI.Core.Server.Authentication.AuthenticationStateMapping");
        CustomStateMapping = SharedReferenceFinder.TryFindByBaseTypeNameStart("gAPI.Core.Server.Authentication.AuthenticationStateMapping", allSymbols);

    }


    public SharedReference FabricClient { get; }
    public SharedReference SseHostCollection { get; }
    public SharedReference ServiceId { get; }
    public SharedReference ServiceMethodId { get; }
    public SharedReference UserId { get; }
    public SharedReference SessionId { get; }
    public SharedReference BaseListResponseT { get; }
    public SharedReference BaseResponseErrorEnum { get; }
    public SharedReference StringHelper { get; }
    public SharedReference BaseResponse { get; }
    public SharedReference BaseResponseT { get; }
    public SharedReference InvokeRequestDto { get; }
    public SharedReference InvokeResponseDto { get; }
    public SharedReference IServerAuthenticationService { get; }
    public SharedReference EmptyServerAuthenticationServiceT { get; }
    public SharedReference IAuthenticationSecurity { get; }
    public SharedReference AuthenticationHandler { get; }
    public SharedReference ISseHost { get; }
    public SharedReference IUseCase { get; }
    public SharedReference Mapping { get; }
    public SharedReference HubResult { get; }
    public SharedReference HubResultT { get; }
    public SharedReference SseEvent { get; }
    public SharedReference ConnectionId { get; }
    public SharedReference AuthenticationInitializeResult { get; }
    public SharedReference AuthenticationHeaders { get; }
    public SharedReference AuthenticationState { get; }
    public SharedReference SendRequestDto { get; }
    public SharedReference ServerConfig { get; }
    public SharedReference SubscribeDto { get; }
    public SharedReference UnsubscribeDto { get; }
    public SharedReference ApiSendRequestDto { get; }
    public SharedReference WssServerConnectionCollection { get; }
    public SharedReference ApiInvokeRequestDto { get; }
    public SharedReference ApiInvokeResponseDto { get; }
    public SharedReference WssSessionCache { get; }
    public SharedReference AccountServiceT { get; }
    public SharedReference AuthenticationSecurityT { get; }
    public SharedReference IAuthenticationStateFactoryT { get; }
    public SharedReference ServerAuthenticationAccessor { get; }
    public SharedReference IStateParserT { get; }
    public SharedReference? IServerAuthenticationServiceImplementation { get; }
    public SharedReference ApiInvokeResponseDoneDto { get; }
    public SharedReference IAccountService { get; }

    public SharedReference AuthenticationDbContextT { get; }
    public SharedReference? CustomDbContext { get; }

    public SharedReference IStateMappingT { get; }
    public SharedReference AuthStateDto { get; }
    public SharedReference? StateDto { get; }

    public SharedReference AuthUser { get; }
    public SharedReference? User { get; }
    public SharedReference AuthenticationStateFactoryT { get; }
    public SharedReference IUserTokenFactoryT { get; }
    public SharedReference UserTokenFactoryT { get; }
    public SharedReference IAuthenticationCheckT { get; }
    public SharedReference Ip { get; }
    public SharedReference UserToken { get; }
    public SharedReference AuthStateUserDto { get; }
    public SharedReference AuthenticationServiceT { get; }
    public SharedReference AuthenticationStateMappingT { get; }
    public SharedReference? CustomStateMapping { get; }
    public SharedReference IAuthenticationServiceT { get; }
}