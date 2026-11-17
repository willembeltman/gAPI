using gAPI.Core.Server.Authentication;
using gAPI.Core.Server.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace gAPI.Core.Server.Interfaces;

public interface IAuthenticationService<TUser, TStateDto>
    : IServerAuthenticationService
    where TUser : AuthUser
{
    TStateDto? ClientState { get; }
    TStateDto State { get; }
    AuthenticationState<TUser> AuthenticationState { get; }
    Task<AsyncServiceScope> CreateNewAuthenticatedScope(CancellationToken ct);
}