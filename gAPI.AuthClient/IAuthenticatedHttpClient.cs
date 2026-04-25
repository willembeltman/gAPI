namespace gAPI.Core.Client;

public interface IAuthenticatedHttpClient<TStateDto> : gAPI.Interfaces.IClientAuthenticatedHttpClient
{
    Task<TStateDto> GetStateAsync(bool force = false, CancellationToken ct = default);
}