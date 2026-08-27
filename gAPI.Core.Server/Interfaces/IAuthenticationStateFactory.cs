using gAPI.Core.Server.Authentication;
using gAPI.Core.Server.Entities;

namespace gAPI.Core.Server.Interfaces;

public interface IAuthenticationStateFactory<TUser>
    where TUser : AuthUser
{
    Task<AuthenticationState<TUser>> CreateAuthenticationStateAsync(AuthenticationHeaders headers, CancellationToken ct);
}