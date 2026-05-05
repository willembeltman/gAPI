using gAPI.Core.Attributes;
using System.ComponentModel.DataAnnotations;

namespace gAPI.Core.Server.Entities;

[IsHidden]
public class Ip<TUser>
    where TUser : AuthUser
{
    public Ip() { }
    public Ip(string ipAdress)
    {
        Address = ipAdress;
    }

    [Key]
    public long Id { get; set; }

    [StringLength(128)]
    public string Address { get; set; } = string.Empty;

    public DateTimeOffset? RegisterLockedOutDate { get; set; }
    public int RegisterCount { get; set; }
    public DateTimeOffset? LoginLockedOutDate { get; set; }
    public int LoginAttempts { get; set; }
    public DateTimeOffset? ForgetPasswordLockedOutDate { get; set; }
    public int ForgetPasswordAttempts { get; set; }
    public int ChangePasswordAttempts { get; set; }
    public DateTimeOffset? ChangePasswordLockedOutDate { get; set; }

    public virtual ICollection<UserIp<TUser>>? UserIps { get; set; }
}