using gAPI.AutoWss.Server.Helpers;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoWss.Server.Models;

public class SharedReferences
{
    public SharedReferences(INamedTypeSymbol[] allSymbols)
    {
        AuthenticationInitializeResult = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.AuthenticationInitializeResult", allSymbols);
        AuthenticationHeaders = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.AuthenticationHeaders", allSymbols);

        FabricClient = SharedReferenceFinder.Find("gAPI.Core.Server.Fabric.FabricClient", allSymbols);

        ServiceId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceId", allSymbols);
        ServiceMethodId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceMethodId", allSymbols);
        UserId = SharedReferenceFinder.Find("gAPI.Core.Ids.UserId", allSymbols);
        SessionId = SharedReferenceFinder.Find("gAPI.Core.Ids.SessionId", allSymbols);

        ServerConfig = SharedReferenceFinder.Find("gAPI.Core.Server.Config.ServerConfig", allSymbols);
        SendRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.SendRequestDto", allSymbols);
        InvokeRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeRequestDto", allSymbols);
        InvokeResponseDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeResponseDto", allSymbols);
        InvokeResponseDoneDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.InvokeResponseDoneDto", allSymbols);

        IServerAuthenticationService = SharedReferenceFinder.Find("gAPI.Core.Interfaces.IServerAuthenticationService", allSymbols);
        
        ServiceSubscriptionCollection = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.ServiceSubscriptionCollection", allSymbols);
        WssServerConnectionCollection = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.WssServerConnectionCollection", allSymbols);
        SessionCache = SharedReferenceFinder.Find("gAPI.Core.Server.Collections.SessionCache", allSymbols);
        
        AuthStateDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.AuthStateDto", allSymbols);
        AuthenticationOptions = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.AuthenticationOptions", allSymbols);
        AuthenticationMiddleware = SharedReferenceFinder.Find("gAPI.Core.Server.Authentication.AuthenticationMiddleware", allSymbols);

    }

    public SharedReference FabricClient { get; }
    public SharedReference ServiceSubscriptionCollection { get; }
    public SharedReference ServiceId { get; }
    public SharedReference ServiceMethodId { get; }
    public SharedReference UserId { get; }
    public SharedReference SessionId { get; }
    public SharedReference InvokeRequestDto { get; }
    public SharedReference InvokeResponseDto { get; }
    public SharedReference IServerAuthenticationService { get; }
    public SharedReference AuthenticationInitializeResult { get; }
    public SharedReference AuthenticationHeaders { get; }
    public SharedReference SendRequestDto { get; }
    public SharedReference ServerConfig { get; }
    public SharedReference WssServerConnectionCollection { get; }
    public SharedReference SessionCache { get; }
    public SharedReference AuthStateDto { get; }
    public SharedReference AuthenticationOptions { get; }
    public SharedReference InvokeResponseDoneDto { get; }
    public SharedReference AuthenticationMiddleware { get; }
}