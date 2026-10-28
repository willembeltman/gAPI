using gAPI.Core.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gAPI.Core.Server.Entities;

[IsHidden]
public class UserIp
{
    public UserIp() { }
    public UserIp(
        Guid? userId,
        long ipId)
    {
        UserId = userId;
        IpId = ipId;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public Guid? UserId { get; set; }

    public long IpId { get; set; }
    public virtual Ip? Ip { get; set; }

    public virtual ICollection<UserIpSession>? UserIpSessions { get; set; }
}