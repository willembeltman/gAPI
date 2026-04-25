using gAPI.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gAPI.Core.Server.Entities;

[IsHidden]
public class UserIp<TUser>
    where TUser : AuthUser
{
    public UserIp() { }
    public UserIp(
        Guid? userId,
        Ip<TUser> ip)
    {
        UserId = userId;
        Ip = ip;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public Guid? UserId { get; set; }
    public virtual TUser? User { get; set; }

    public long IpId { get; set; }
    public virtual Ip<TUser>? Ip { get; set; }

    public virtual ICollection<UserIpSession<TUser>>? UserIpSessions { get; set; }
}