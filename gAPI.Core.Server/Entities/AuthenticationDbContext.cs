using Microsoft.EntityFrameworkCore;

namespace gAPI.Core.Server.Entities;

public class AuthenticationDbContext<TUser> : DbContext
    where TUser : AuthUser
{
    public AuthenticationDbContext(DbContextOptions options) : base(options)
    {
    }

    public virtual DbSet<TUser> Users { get; set; }
    public virtual DbSet<Route> Routes { get; set; }
    public virtual DbSet<Session> Sessions { get; set; }
    public virtual DbSet<Ip> Ips { get; set; }
    public virtual DbSet<UserToken> Tokens { get; set; }
    public virtual DbSet<UserIp> UserIps { get; set; }
    public virtual DbSet<UserIpSession> UserIpSessions { get; set; }
    public virtual DbSet<UserIpSessionToken> UserIpSessionTokens { get; set; }
    public virtual DbSet<UserIpSessionTokenRoute> UserIpSessionTokenRoutes { get; set; }
    public virtual DbSet<UserIpSessionTokenRouteRequest> UserIpSessionTokenRouteRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserToken>()
            .HasIndex(a => a.UserId);
        modelBuilder.Entity<UserIp>()
            .HasIndex(a => a.UserId);

        modelBuilder.Entity<UserIp>()
            .HasOne(cb => cb.Ip)
            .WithMany(cd => cd.UserIps)
            .HasForeignKey(cb => cb.IpId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserIpSessionToken>()
            .HasOne(cb => cb.UserIpSession)
            .WithMany(cd => cd.UserIpSessionTokens)
            .HasForeignKey(cb => cb.UserIpSessionId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserIpSessionToken>()
            .HasOne(cb => cb.Token)
            .WithMany(cd => cd.UserIpSessionTokens)
            .HasForeignKey(cb => cb.TokenId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserIpSession>()
            .HasOne(cb => cb.UserIp)
            .WithMany(cd => cd.UserIpSessions)
            .HasForeignKey(cb => cb.UserIpId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserIpSession>()
            .HasOne(cb => cb.Session)
            .WithMany(cd => cd.UserIpSessions)
            .HasForeignKey(cb => cb.SessionId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserIpSessionTokenRoute>()
            .HasOne(cb => cb.UserIpSessionToken)
            .WithMany(cd => cd.UserIpSessionTokenRoutes)
            .HasForeignKey(cb => cb.UserIpSessionTokenId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserIpSessionTokenRoute>()
            .HasOne(cb => cb.Route)
            .WithMany(cd => cd.UserIpSessionTokenRoutes)
            .HasForeignKey(cb => cb.RouteId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserIpSessionTokenRouteRequest>()
            .HasOne(cb => cb.UserIpSessionTokenRoute)
            .WithMany(cd => cd.UserIpSessionTokenRouteRequests)
            .HasForeignKey(cb => cb.UserIpSessionTokenRouteId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}