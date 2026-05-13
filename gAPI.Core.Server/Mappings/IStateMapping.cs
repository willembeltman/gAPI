using gAPI.Core.Dtos;
using gAPI.Core.Server.Entities;

namespace gAPI.Core.Server.Mappings;

public interface IStateMapping<TUser, TStateDto>
        where TUser : AuthUser
        where TStateDto : AuthStateDto, new()
{
    Task<TStateDto> ToDtoAsync(
        TUser? dbUser,
        UserToken<TUser>? dbToken,
        Ip<TUser> dbIp,
        TStateDto? receivedClientState, // <-- IMPORTANT: DO NOT TRUST THIS STATE
        CancellationToken ct);
}