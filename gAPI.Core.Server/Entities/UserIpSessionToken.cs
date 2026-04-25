using gAPI.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gAPI.Core.Server.Entities;

[IsHidden]
public class UserIpSessionToken<TUser>
    where TUser : AuthUser
{
    public UserIpSessionToken() { }
    public UserIpSessionToken(
        UserIpSession<TUser> userIpSessionCompany,
        UserToken<TUser>? token)
    {
        UserIpSession = userIpSessionCompany;
        Token = token;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long? TokenId { get; set; }
    public virtual UserToken<TUser>? Token { get; set; }

    public long UserIpSessionId { get; set; }
    public virtual UserIpSession<TUser>? UserIpSession { get; set; }

    public virtual ICollection<UserIpSessionTokenRoute<TUser>>? UserIpSessionTokenRoutes { get; set; }
}
