using gAPI.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Timers;

namespace gAPI.Core.Server.Entities;

[IsHidden]
public class Session<TUser>
    where TUser : AuthUser
{
    public Session() { }
    public Session(string sessionId)
    {
        SessionId = sessionId;
    }

    [Key]
    public long Id { get; set; }

    [StringLength(256)]
    public string SessionId { get; set; } = string.Empty;

    public virtual ICollection<UserIpSession<TUser>>? UserIpSessions { get; set; }
}