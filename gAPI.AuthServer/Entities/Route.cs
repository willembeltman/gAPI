using gAPI.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Timers;

namespace gAPI.Core.Server.Entities;

[IsHidden]
public class Route<TUser>
    where TUser : AuthUser
{
    public Route() { }
    public Route(string route)
    {
        RouteName = route;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public string? RouteName { get; set; } = string.Empty;

    public virtual ICollection<UserIpSessionTokenRoute<TUser>>? UserIpSessionTokenRoutes { get; set; }
}