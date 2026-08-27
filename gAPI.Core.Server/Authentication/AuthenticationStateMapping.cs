using gAPI.Core.Dtos;
using gAPI.Core.Server.Entities;
using gAPI.Core.Server.Interfaces;

namespace gAPI.Core.Server.Authentication;

public class AuthenticationStateMapping<TUser, TStateDto>
    : IStateMapping<TUser, TStateDto>
    where TUser : AuthUser
    where TStateDto : AuthStateDto, new()
{
    public virtual async Task<TStateDto> ToDtoAsync(
        TUser? dbUser, 
        UserToken<TUser>? dbToken, 
        Ip<TUser> dbIp,
        TStateDto? receivedClientState, 
        CancellationToken ct)
    {
        return new TStateDto
        {
            User = dbUser != null ? await ToDtoAsync(dbUser, new AuthStateUserDto(), ct) : null
        };
    }
    public async Task<AuthStateUserDto> ToDtoAsync(
        TUser dbUser,
        AuthStateUserDto dto,
        CancellationToken ct)
    {
        dto.Id = dbUser.Id;
        dto.UserName = dbUser.UserName;
        dto.Email = dbUser.Email;
        //dto.StorageFileUrl = await storageService.GetStorageFileUrlAsync($"User/{dto.Id}", ct);
        return dto;
    }
}
