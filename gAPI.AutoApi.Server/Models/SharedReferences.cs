using gAPI.AutoApi.Server.Helpers;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoApi.Server.Models;

public class SharedReferences
{
    public SharedReferences(INamedTypeSymbol[] allSymbols)
    {
        AuthenticationInitializeResult = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.AuthenticationInitializeResult", allSymbols);
        FabricClient = SharedReferenceFinder.Find("gAPI.Core.Server.Fabric.FabricClient", allSymbols);
        ServerConfig = SharedReferenceFinder.Find("gAPI.Core.Dtos.ServerConfig", allSymbols);
        IServerAuthenticationService = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IServerAuthenticationService", allSymbols);
        SseHostCollection = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.SseHostCollection", allSymbols);
        AuthServer_Middleware = SharedReferenceFinder.TryFindStart("gAPI.Core.Server.AuthenticationMiddleware", allSymbols);
    }

    public SharedReference FabricClient { get; }
    public SharedReference SseHostCollection { get; }
    public SharedReference IServerAuthenticationService { get; }
    public SharedReference AuthenticationInitializeResult { get; }
    public SharedReference ServerConfig { get; }
    public SharedReference? AuthServer_Middleware { get; }
}