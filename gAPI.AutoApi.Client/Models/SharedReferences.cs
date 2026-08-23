using gAPI.AutoApi.Client.Helpers;
using Microsoft.CodeAnalysis;

namespace gAPI.AutoApi.Client.Models;

public class SharedReferences
{
    public SharedReferences(INamedTypeSymbol[] allSymbols)
    {
        BaseResponseT = new SharedReference("gAPI.Core.Dtos.BaseResponseT"); //Find("gAPI.Core.Dtos.BaseResponseT", allSymbols);
        BaseResponse = SharedReferenceFinder.Find("gAPI.Core.Dtos.BaseResponse", allSymbols);
        IClientAuthenticatedHttpClient = SharedReferenceFinder.Find("gAPI.Core.Client.Interfaces.IClientAuthenticatedHttpClient", allSymbols);

        AuthClient_FormFile = SharedReferenceFinder.TryFindByAttribute("gAPI.Core.Attributes.IsFormFileAttribute", allSymbols);
        AuthClient_ToFormFileExtension = SharedReferenceFinder.TryFindByAttribute("gAPI.Core.Attributes.IsFormFileExtensionAttribute", allSymbols);
    }
    public SharedReference BaseResponse { get; }
    public SharedReference BaseResponseT { get; }
    public SharedReference IClientAuthenticatedHttpClient { get; }
    public SharedReference? AuthClient_FormFile { get; }
    public SharedReference? AuthClient_ToFormFileExtension { get; }
}