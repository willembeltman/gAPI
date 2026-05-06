using gAPI.Core.Attributes;
using gAPI.Core.Server.Storage;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gAPI.Core.Server.Entities;

public class AuthenticationDbContext<TUser> : DbContext
    where TUser : AuthUser
{
    public AuthenticationDbContext(DbContextOptions options) : base(options)
    {
    }

    public virtual DbSet<TUser> Users { get; set; }
    public virtual DbSet<Route<TUser>> Routes { get; set; }
    public virtual DbSet<Session<TUser>> Sessions { get; set; }
    public virtual DbSet<Ip<TUser>> Ips { get; set; }
    public virtual DbSet<UserToken<TUser>> Tokens { get; set; }
    public virtual DbSet<UserIp<TUser>> UserIps { get; set; }
    public virtual DbSet<UserIpSession<TUser>> UserIpSessions { get; set; }
    public virtual DbSet<UserIpSessionToken<TUser>> UserIpSessionTokens { get; set; }
    public virtual DbSet<UserIpSessionTokenRoute<TUser>> UserIpSessionTokenRoutes { get; set; }
    public virtual DbSet<UserIpSessionTokenRouteRequest<TUser>> UserIpSessionTokenRouteRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserIp<TUser>>()
            .HasOne(cb => cb.Ip)
            .WithMany(cd => cd.UserIps)
            .HasForeignKey(cb => cb.IpId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserIpSessionToken<TUser>>()
            .HasOne(cb => cb.UserIpSession)
            .WithMany(cd => cd.UserIpSessionTokens)
            .HasForeignKey(cb => cb.UserIpSessionId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserIpSessionToken<TUser>>()
            .HasOne(cb => cb.Token)
            .WithMany(cd => cd.UserIpSessionTokens)
            .HasForeignKey(cb => cb.TokenId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserIpSession<TUser>>()
            .HasOne(cb => cb.UserIp)
            .WithMany(cd => cd.UserIpSessions)
            .HasForeignKey(cb => cb.UserIpId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserIpSession<TUser>>()
            .HasOne(cb => cb.Session)
            .WithMany(cd => cd.UserIpSessions)
            .HasForeignKey(cb => cb.SessionId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserIpSessionTokenRoute<TUser>>()
            .HasOne(cb => cb.UserIpSessionToken)
            .WithMany(cd => cd.UserIpSessionTokenRoutes)
            .HasForeignKey(cb => cb.UserIpSessionTokenId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserIpSessionTokenRoute<TUser>>()
            .HasOne(cb => cb.Route)
            .WithMany(cd => cd.UserIpSessionTokenRoutes)
            .HasForeignKey(cb => cb.RouteId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserIpSessionTokenRouteRequest<TUser>>()
            .HasOne(cb => cb.UserIpSessionTokenRoute)
            .WithMany(cd => cd.UserIpSessionTokenRouteRequests)
            .HasForeignKey(cb => cb.UserIpSessionTokenRouteId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Ip<TUser>>().ToTable("Ips");
        modelBuilder.Entity<Route<TUser>>().ToTable("Routes");
        modelBuilder.Entity<Session<TUser>>().ToTable("Sessions");
        modelBuilder.Entity<UserToken<TUser>>().ToTable("UserTokens");
        modelBuilder.Entity<UserIp<TUser>>().ToTable("UserIps");
        modelBuilder.Entity<UserIpSession<TUser>>().ToTable("UserIpSessions");
        modelBuilder.Entity<UserIpSessionToken<TUser>>().ToTable("UserIpSessionTokens");
        modelBuilder.Entity<UserIpSessionTokenRoute<TUser>>().ToTable("UserIpSessionTokenRoutes");
        modelBuilder.Entity<UserIpSessionTokenRouteRequest<TUser>>()
            .ToTable("UserIpSessionTokenRouteRequests");
    }
}
[IsAuthorized]
[IsUser]
[IsEntryPoint]
public class AuthUser : IStorageFile
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [StringLength(128)]
    [Required]
    [IsHidden]
    public string? PasswordHash { get; set; }
    [IsHidden]
    public bool LockedOut { get; set; }

    [IsName]
    [IsState]
    [StringLength(128)]
    [Required]
    public string UserName { get; set; } = string.Empty;
    [IsState]
    [StringLength(255)]
    [Required]
    public string Email { get; set; } = string.Empty;
    [StringLength(32)]
    public string PhoneNumber { get; set; } = string.Empty;

    [IsState]
    string IStorageFile.Id => Id.ToString();
}

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

[IsHidden]
public class UserIpSessionToken<TUser>
    where TUser : AuthUser
{
    public UserIpSessionToken() { }
    public UserIpSessionToken(
        UserIpSession<TUser> userIpSessionCompany,
        UserToken<TUser>? token)
    {
        UserIpSession = userIpSessionCompany;
        Token = token;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long? TokenId { get; set; }
    public virtual UserToken<TUser>? Token { get; set; }

    public long UserIpSessionId { get; set; }
    public virtual UserIpSession<TUser>? UserIpSession { get; set; }

    public virtual ICollection<UserIpSessionTokenRoute<TUser>>? UserIpSessionTokenRoutes { get; set; }
}

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

[IsHidden]
public class UserToken<TUser>
    where TUser : AuthUser
{
    public UserToken() { }
    public UserToken(Guid userId, string tokenHash)
    {
        UserId = userId;
        TokenHash = tokenHash;
    }

    [Key]
    public long Id { get; set; }

    public Guid UserId { get; set; }
    public virtual TUser User { get; set; } = default!;

    [StringLength(280)]
    public string TokenHash { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;

    public virtual ICollection<UserIpSessionToken<TUser>>? UserIpSessionTokens { get; set; }
}