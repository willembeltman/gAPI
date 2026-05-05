using gAPI.Core.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gAPI.Core.Server.Entities;

[IsHidden]
public class UserIpSession<TUser>
    where TUser : AuthUser
{
    public UserIpSession() { }
    public UserIpSession(
        UserIp<TUser> userIp,
        Session<TUser> session)
    {
        UserIp = userIp;
        Session = session;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long UserIpId { get; set; }
    public virtual UserIp<TUser>? UserIp { get; set; }

    public long SessionId { get; set; }
    public virtual Session<TUser>? Session { get; set; }

    public virtual ICollection<UserIpSessionToken<TUser>>? UserIpSessionTokens { get; set; }
}