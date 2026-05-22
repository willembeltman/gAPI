using gAPI.Core.Dtos;
using gAPI.Core.Server.Entities;
using gAPI.Core.Server.Storage;

namespace gAPI.Core.Server.Mappings;

public class AuthStateUserMapping(
    IStorageService storageService)
    : IStateUserMapping<AuthUser, AuthStateUserDto>
{
    public async Task<AuthStateUserDto> ToDtoAsync(
        AuthUser entity,
        AuthStateUserDto dto,
        CancellationToken ct)
    {
        dto.Id = entity.Id;
        dto.UserName = entity.UserName;
        dto.Email = entity.Email;
        dto.StorageFileUrl = await storageService.GetStorageFileUrlAsync($"User/{dto.Id}", ct);
        return dto;
    }
}