using gAPI.Core.Server.Entities;
using gAPI.Dtos;
using gAPI.Storage;

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
        dto.StorageFileUrl = await storageService.GetStorageFileUrlAsync(dto.Id.ToString(), "User", ct);
        return dto;
    }
}