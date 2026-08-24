namespace gAPI.Core.Client.Interfaces;

public interface IAuthenticatedHttpClient<TStateDto> : IClientAuthenticatedHttpClient
{
    Task<TStateDto> GetStateAsync(bool force = false, CancellationToken ct = default);
}