using System.ComponentModel.DataAnnotations;

namespace gAPI.Core.Dtos;

public class AuthStateUserDto 
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}