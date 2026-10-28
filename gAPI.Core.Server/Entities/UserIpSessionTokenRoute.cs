using gAPI.Core.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gAPI.Core.Server.Entities;

[IsHidden]
public class UserIpSessionTokenRoute
{
    public UserIpSessionTokenRoute() { }
    public UserIpSessionTokenRoute(
        UserIpSessionToken? userIpSessionCompanyToken,
        Route? route)
    {
        UserIpSessionToken = userIpSessionCompanyToken;
        Route = route;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long UserIpSessionTokenId { get; set; }
    public virtual UserIpSessionToken? UserIpSessionToken { get; set; }

    public long RouteId { get; set; }
    public virtual Route? Route { get; set; }

    public virtual ICollection<UserIpSessionTokenRouteRequest>? UserIpSessionTokenRouteRequests { get; set; }
}