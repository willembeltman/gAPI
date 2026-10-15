using gAPI.Core.Server.Authentication;
using gAPI.Core.Server.Entities;

namespace gAPI.Core.Server.Interfaces;

public interface IAuthenticationService<TUser, TStateDto>
    : IServerAuthenticationService
    where TUser : AuthUser
{
    TStateDto? ClientState { get; }
    TStateDto State { get; }
    AuthenticationState<TUser> AuthenticationState { get; }
}