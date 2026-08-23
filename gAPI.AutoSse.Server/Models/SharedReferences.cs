using gAPI.AutoSse.Server.Helpers;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoSse.Server.Models;

public class SharedReferences
{
    public SharedReferences(INamedTypeSymbol[] allSymbols)
    {
        FabricClient = SharedReferenceFinder.Find("gAPI.Core.Server.Fabric.FabricClient", allSymbols);
        SseHost = SharedReferenceFinder.Find("gAPI.Core.Sse.SseHost", allSymbols);
        ServiceId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceId", allSymbols);
        ServiceMethodId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceMethodId", allSymbols);
        UserId = SharedReferenceFinder.Find("gAPI.Core.Ids.UserId", allSymbols);
        SessionId = SharedReferenceFinder.Find("gAPI.Core.Ids.SessionId", allSymbols);
        ServerConfig = SharedReferenceFinder.Find("gAPI.Core.Dtos.ServerConfig", allSymbols);
        IServerAuthenticationService = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IServerAuthenticationService", allSymbols);
        SseHostCollection = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.SseHostCollection", allSymbols);
        WssSessionCache = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.WssSessionCache", allSymbols);
    }

    public SharedReference FabricClient { get; }
    public SharedReference SseHostCollection { get; }
    public SharedReference SseHost { get; }
    public SharedReference ServiceId { get; }
    public SharedReference ServiceMethodId { get; }
    public SharedReference UserId { get; }
    public SharedReference SessionId { get; }
    public SharedReference IServerAuthenticationService { get; }
    public SharedReference ServerConfig { get; }
    public SharedReference WssSessionCache { get; }
}