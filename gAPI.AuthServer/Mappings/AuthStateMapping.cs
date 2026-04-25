using gAPI.Core.Server.Entities;
using gAPI.Dtos;

namespace gAPI.Core.Server.Mappings;

public class AuthStateMapping (
    IStateUserMapping<AuthUser, AuthStateUserDto> userMapping)
    : IStateMapping<AuthUser, AuthStateDto>
{
    public async Task<AuthStateDto> ToDtoAsync(AuthUser? dbUser, UserToken<AuthUser>? dbToken, Ip<AuthUser> dbIp, AuthStateDto? receivedClientState, CancellationToken ct)
    {
        return new AuthStateDto
        {
            User = dbUser != null ? await userMapping.ToDtoAsync(dbUser, new AuthStateUserDto(), ct) : null
        };
    }
}
