using gAPI.Authentication;
using gAPI.Core.Server.Entities;
using gAPI.Dtos;

namespace gAPI.Core.Server;

public interface IAuthenticationStateFactory<TUser, TStateDto>
    where TUser : AuthUser
{
    Task<(TStateDto, AuthenticationState<TUser>)> CreateAuthenticationStateAsync(AuthenticationHeaders headers, TStateDto? stateData, CancellationToken ct);
    Task SaveTokenAsync(string userId, string cookieHash, CancellationToken ct);
}