using gAPI.AutoApi.Server.Helpers;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoApi.Server.Models;

public class SharedReferences
{
    public SharedReferences(INamedTypeSymbol[] allSymbols)
    {
        AuthenticationInitializeResult = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.AuthenticationInitializeResult", allSymbols);
        AuthServer_Middleware = SharedReferenceFinder.TryFindStart("gAPI.Core.Server.AuthenticationMiddleware", allSymbols);

        FabricClient = SharedReferenceFinder.Find("gAPI.Core.Server.Fabric.FabricClient", allSymbols);
        SseServiceSubscription = SharedReferenceFinder.Find("gAPI.Core.Sse.SseServiceSubscription", allSymbols);
        ServiceId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceId", allSymbols);
        ServiceMethodId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceMethodId", allSymbols);
        UserId = SharedReferenceFinder.Find("gAPI.Core.Ids.UserId", allSymbols);
        SessionId = SharedReferenceFinder.Find("gAPI.Core.Ids.SessionId", allSymbols);
        ServerConfig = SharedReferenceFinder.Find("gAPI.Core.Dtos.ServerConfig", allSymbols);
        IServerAuthenticationService = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IServerAuthenticationService", allSymbols);
        ServiceSubscriptionCollection = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.ServiceSubscriptionCollection", allSymbols);
        SessionCache = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.SessionCache", allSymbols);
        AuthenticationOptions = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.AuthenticationOptions", allSymbols);
    }

    public SharedReference AuthenticationInitializeResult { get; }
    public SharedReference? AuthServer_Middleware { get; }

    public SharedReference FabricClient { get; }
    public SharedReference ServiceSubscriptionCollection { get; }
    public SharedReference SseServiceSubscription { get; }
    public SharedReference ServiceId { get; }
    public SharedReference ServiceMethodId { get; }
    public SharedReference UserId { get; }
    public SharedReference SessionId { get; }
    public SharedReference IServerAuthenticationService { get; }
    public SharedReference ServerConfig { get; }
    public SharedReference SessionCache { get; }
    public SharedReference AuthenticationOptions { get; }
}