using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace gAPI.Core.Client;

/// <summary>
/// Handler to ensure cookie credentials are automatically sent over with each request.
/// </summary>
public sealed class WithCookiesHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        return await base.SendAsync(request, cancellationToken);
    }
}