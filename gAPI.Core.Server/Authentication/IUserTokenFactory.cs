using gAPI.Core.Server.Entities;

namespace gAPI.Core.Server.Authentication;

public interface IUserTokenFactory<TUser>
    where TUser : AuthUser
{
    Task<UserToken<TUser>> SaveTokenAsync(string userId, string cookieHash, CancellationToken ct);
}