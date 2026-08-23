using gAPI.AutoSse.Client.Helpers;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoSse.Client.Models;

public class SharedReferences
{
    public SharedReferences(INamedTypeSymbol[] allSymbols)
    {
        SendRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.SendRequestDto", allSymbols);
        ServiceId = SharedReferenceFinder.Find("gAPI.Core.Ids.ServiceId", allSymbols);
        SseManagerId = SharedReferenceFinder.Find("gAPI.Core.Ids.SseManagerId", allSymbols);
        SendRequestDto = SharedReferenceFinder.Find("gAPI.Core.Dtos.SendRequestDto", allSymbols);
        IClientAuthenticatedHttpClient = SharedReferenceFinder.Find("gAPI.Core.Client.Interfaces.IClientAuthenticatedHttpClient", allSymbols);
        ISseClientConnection = SharedReferenceFinder.Find("gAPI.Core.Client.Interfaces.ISseClientConnection", allSymbols);

        SseManagerCollection = SharedReferenceFinder.Find("gAPI.Core.Client.Collections.SseManagerCollection", allSymbols);
        SseClient = SharedReferenceFinder.Find("gAPI.Core.Client.Sse.SseClient", allSymbols);
    }
    public SharedReference ServiceId { get; }
    public SharedReference SendRequestDto { get; }
    public SharedReference IClientAuthenticatedHttpClient { get; }
    public SharedReference SseManagerCollection { get; }
    public SharedReference ISseClientConnection { get; }
    public SharedReference SseManagerId { get; }
    public SharedReference SseClient { get; }
}