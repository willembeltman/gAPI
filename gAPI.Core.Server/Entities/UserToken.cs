using gAPI.Core.Attributes;
using System.ComponentModel.DataAnnotations;

namespace gAPI.Core.Server.Entities;

[IsHidden]
public class UserToken<TUser>
    where TUser : AuthUser
{
    public UserToken() { }
    public UserToken(Guid userId, string tokenHash)
    {
        UserId = userId;
        TokenHash = tokenHash;
    }

    [Key]
    public long Id { get; set; }

    public Guid UserId { get; set; }
    public virtual TUser User { get; set; } = default!;

    [StringLength(280)]
    public string TokenHash { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;

    public virtual ICollection<UserIpSessionToken<TUser>>? UserIpSessionTokens { get; set; }
}