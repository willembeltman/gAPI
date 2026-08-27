using gAPI.Core.Dtos;
using gAPI.Core.Server.Authentication;
using gAPI.Core.Server.Entities;

namespace gAPI.Core.Server.Interfaces;

public interface IAuthenticationCheck<TUser, TStateDto>
    where TUser : AuthUser
    where TStateDto : AuthStateDto, new()
{
    bool IsValid(
        AuthenticationHeaders headers,
        TStateDto? receivedClientState,
        TStateDto state,
        AuthenticationState<TUser> authenticationState,
        out AuthenticationInitializeResult notValidResult);
}