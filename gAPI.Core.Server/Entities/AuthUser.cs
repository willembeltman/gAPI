using gAPI.Core.Attributes;
using gAPI.Core.Server.Storage;
using System.ComponentModel.DataAnnotations;

namespace gAPI.Core.Server.Entities
{
    [IsAuthorized]
    [IsUser]
    [IsEntryPoint]
    public class AuthUser : IStorageFile
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [StringLength(128)]
        [Required]
        [IsHidden]
        public string? PasswordHash { get; set; }
        [IsHidden]
        public bool LockedOut { get; set; }

        [IsName]
        [IsState]
        [StringLength(128)]
        [Required]
        public string UserName { get; set; } = string.Empty;
        [IsState]
        [StringLength(255)]
        [Required]
        public string Email { get; set; } = string.Empty;
        [StringLength(32)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [IsState]
        string IStorageFile.Id => Id.ToString();
    }
}
