using gAPI.Core.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gAPI.Core.Server.Entities;

[IsHidden]
public class UserIpSession
{
    public UserIpSession() { }
    public UserIpSession(
        UserIp userIp,
        Session session)
    {
        UserIpId = userIp.Id;
        SessionId = session.Id;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long UserIpId { get; set; }
    public virtual UserIp? UserIp { get; set; }

    public long SessionId { get; set; }
    public virtual Session? Session { get; set; }

    public virtual ICollection<UserIpSessionToken>? UserIpSessionTokens { get; set; }
}