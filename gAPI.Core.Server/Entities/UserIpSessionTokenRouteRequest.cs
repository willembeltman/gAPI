using gAPI.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gAPI.Core.Server.Entities;

[IsHidden]
public class UserIpSessionTokenRouteRequest<TUser>
    where TUser : AuthUser
{
    public UserIpSessionTokenRouteRequest() { }
    public UserIpSessionTokenRouteRequest(
        UserIpSessionTokenRoute<TUser>? userIpSessionCompanyTokenRoute,
        DateTimeOffset date)
    {
        UserIpSessionTokenRoute = userIpSessionCompanyTokenRoute;
        Year = Convert.ToInt16(date.Year);
        Month = Convert.ToByte(date.Month);
        Day = Convert.ToByte(date.Day);
        Hour = Convert.ToByte(date.Hour);
        Count = 1;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long UserIpSessionTokenRouteId { get; set; }
    public virtual UserIpSessionTokenRoute<TUser>? UserIpSessionTokenRoute { get; set; }

    public short Year { get; set; }
    public byte Month { get; set; }
    public byte Day { get; set; }
    public byte Hour { get; set; }

    public int Count { get; set; }
}