using gAPI.Core.Server.Entities;

namespace gAPI.Core.Server;

public interface IAuthenticationService<TUser, TStateDto> 
    : gAPI.Core.Interfaces.IServerAuthenticationService
    where TUser : AuthUser
{
    TStateDto? ClientState { get; }
    TStateDto State { get; }
    AuthenticationState<TUser> AuthenticationState { get; }
}