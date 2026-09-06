using gAPI.AutoWss.Server.Helpers;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace gAPI.AutoWss.Server.Models;

public class SharedReferences
{
    public SharedReferences(ServiceContext serviceContext, INamedTypeSymbol[] allSymbols)
    {
        AuthenticationInitializeResult = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.AuthenticationInitializeResult", allSymbols);
        AuthenticationHeaders = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.AuthenticationHeaders", allSymbols);

        FabricClient = SharedReferenceFinder.Find("gAPI.Core.Server.Fabric.FabricClient", allSymbols);

        ServiceId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceId", allSymbols);
        ServiceMethodId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceMethodId", allSymbols);
        UserId = SharedReferenceFinder.Find("gAPI.Core.Ids.UserId", allSymbols);
        SessionId = SharedReferenceFinder.Find("gAPI.Core.Ids.SessionId", allSymbols);
        RequestId = SharedReferenceFinder.Find("gAPI.Core.Ids.RequestId", allSymbols);

        ServerConfig = SharedReferenceFinder.Find("gAPI.Core.Server.Config.ServerConfig", allSymbols);
        RoutingDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.RoutingDto", allSymbols);
        SendRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.SendRequestDto", allSymbols);
        InvokeRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeRequestDto", allSymbols);
        StreamingResponseDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.StreamingResponseDto", allSymbols);
        InvokeRequestDoneDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeRequestDoneDto", allSymbols);

        IServerAuthenticationService = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IServerAuthenticationService", allSymbols);
        
        ServiceSubscriptionCollection = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.ServiceSubscriptionCollection", allSymbols);
        ServerConnectionCollection = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.ServerConnectionCollection", allSymbols);
        SessionCache = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.SessionCache", allSymbols);
        StreamingCache = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.StreamingCache", allSymbols);
        
        AuthStateDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.AuthStateDto", allSymbols);
        AuthenticationOptions = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.AuthenticationOptions", allSymbols);
        AuthenticationMiddleware = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.AuthenticationMiddleware", allSymbols);
        WssServerConnection = SharedReferenceFinder.Find("gAPI.Core.Server.Wss.WssServerConnection", allSymbols);

        OwnServerConnection = SharedReferenceFinder.TryFindByBaseType(WssServerConnection, allSymbols);

        OwnHubs = serviceContext.HubInterfaces
            .Select(a => new { Interface = a, Implementation = SharedReferenceFinder.TryFindByInterface(a, allSymbols) })
            .Where(a => a.Implementation != null)
            .ToDictionary(a => a.Interface, a => a.Implementation!);

    }

    public SharedReference FabricClient { get; }
    public SharedReference ServiceSubscriptionCollection { get; }
    public SharedReference ServiceId { get; }
    public SharedReference ServiceMethodId { get; }
    public SharedReference UserId { get; }
    public SharedReference SessionId { get; }
    public SharedReference RequestId { get; }
    public SharedReference InvokeRequestDto { get; }
    public SharedReference StreamingResponseDto { get; }
    public SharedReference IServerAuthenticationService { get; }
    public SharedReference AuthenticationInitializeResult { get; }
    public SharedReference AuthenticationHeaders { get; }
    public SharedReference SendRequestDto { get; }
    public SharedReference ServerConfig { get; }
    public SharedReference RoutingDto { get; }
    public SharedReference ServerConnectionCollection { get; }
    public SharedReference SessionCache { get; }
    public SharedReference StreamingCache { get; }
    public SharedReference AuthStateDto { get; }
    public SharedReference AuthenticationOptions { get; }
    public SharedReference InvokeRequestDoneDto { get; }
    public SharedReference AuthenticationMiddleware { get; }
    public SharedReference WssServerConnection { get; }
    public SharedReference? OwnServerConnection { get; }
    public Dictionary<Interface, SharedReference> OwnHubs { get; }
}