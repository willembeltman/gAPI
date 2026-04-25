using gAPI.Core.Server.Entities;
using Microsoft.EntityFrameworkCore;

namespace gAPI.Core.Server;

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
    }
}
