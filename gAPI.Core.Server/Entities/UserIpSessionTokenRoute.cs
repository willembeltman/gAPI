using gAPI.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gAPI.Core.Server.Entities;

[IsHidden]
public class UserIpSessionTokenRoute<TUser>
    where TUser : AuthUser
{
    public UserIpSessionTokenRoute() { }
    public UserIpSessionTokenRoute(
        UserIpSessionToken<TUser>? userIpSessionCompanyToken,
        Route<TUser>? route)
    {
        UserIpSessionToken = userIpSessionCompanyToken;
        Route = route;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long UserIpSessionTokenId { get; set; }
    public virtual UserIpSessionToken<TUser>? UserIpSessionToken { get; set; }

    public long RouteId { get; set; }
    public virtual Route<TUser>? Route { get; set; }

    public virtual ICollection<UserIpSessionTokenRouteRequest<TUser>>? UserIpSessionTokenRouteRequests { get; set; }
}