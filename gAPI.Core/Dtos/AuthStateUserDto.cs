using gAPI.Core.Attributes;
using gAPI.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace gAPI.Core.Dtos;

public class AuthStateUserDto : IStorageFileDto
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [IsStorageFileUrlProperty]
    public string? StorageFileUrl { get; set; }
}