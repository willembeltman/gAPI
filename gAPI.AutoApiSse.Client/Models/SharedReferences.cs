using gAPI.AutoApiSse.Client.Helpers;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoApiSse.Client.Models;

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

        AuthClient_FormFile = SharedReferenceFinder.TryFindByAttribute("gAPI.Core.Attributes.IsFormFileAttribute", allSymbols);
        AuthClient_ToFormFileExtension = SharedReferenceFinder.TryFindByAttribute("gAPI.Core.Attributes.IsFormFileExtensionAttribute", allSymbols);
    }
    public SharedReference ServiceId { get; }
    public SharedReference SendRequestDto { get; }
    public SharedReference IClientAuthenticatedHttpClient { get; }
    public SharedReference SseManagerCollection { get; }
    public SharedReference ISseClientConnection { get; }
    public SharedReference SseManagerId { get; }
    public SharedReference SseClient { get; }
    public SharedReference? AuthClient_FormFile { get; }
    public SharedReference? AuthClient_ToFormFileExtension { get; }
}