using gAPI.Core.Server.Authentication;
using gAPI.Core.Server.Entities;

namespace gAPI.Core.Server;

public interface IAuthenticationStateFactory<TUser, TStateDto>
    where TUser : AuthUser
{
    Task<(TStateDto, AuthenticationState<TUser>)> CreateAuthenticationStateAsync(AuthenticationHeaders headers, TStateDto? stateData, CancellationToken ct);
    Task SaveTokenAsync(string userId, string cookieHash, CancellationToken ct);
}