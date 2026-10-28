using gAPI.Core.Attributes;
using System.ComponentModel.DataAnnotations;

namespace gAPI.Core.Server.Entities;

[IsHidden]
public class UserToken
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

    [StringLength(280)]
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;

    public virtual ICollection<UserIpSessionToken>? UserIpSessionTokens { get; set; }
}