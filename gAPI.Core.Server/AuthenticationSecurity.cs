using gAPI.Core.Server.Entities;
using gAPI.Core.Server.Dtos;
using Microsoft.EntityFrameworkCore;

namespace gAPI.Core.Server;

public class AuthenticationSecurity<TUser, TStateDto>(
    IAuthenticationService<TUser, TStateDto> authentication,
    IDbContextFactory<AuthenticationDbContext<TUser>> dbFactory,
    TimeProvider timeProvider,
    ServerConfig config) 
    : gAPI.Core.Interfaces.IAuthenticationSecurity
    where TUser : AuthUser
{
    public async Task<bool> BeforeLoginAsync(CancellationToken ct)
    {
        if (authentication.AuthenticationState == null || authentication.AuthenticationState.Ip == null)
            throw new Exception("Initialize the ServerAuthenticationService first please");

        var now = timeProvider.GetUtcNow();
        var lockedOutUntil = authentication.AuthenticationState.Ip.LoginLockedOutDate;

        if (lockedOutUntil.HasValue && lockedOutUntil.Value > now)
            return false; // Geblokkeerd

        return true; // Niet geblokkeerd, mag proberen
    }
    public async Task<bool> BeforeChangePasswordAsync(CancellationToken ct)
    {
        if (authentication.AuthenticationState == null || authentication.AuthenticationState.Ip == null)
            throw new Exception("Initialize the ServerAuthenticationService first please");

        var now = timeProvider.GetUtcNow();
        var lockedOutUntil = authentication.AuthenticationState.Ip.ChangePasswordLockedOutDate;

        if (lockedOutUntil.HasValue && lockedOutUntil.Value > now)
            return false; // Geblokkeerd

        return true; // Niet geblokkeerd, mag proberen
    }
    public async Task<bool> AfterSuccesfullLoginAsync(CancellationToken ct) =>
        await CheckAndUpdateIpLockout(
            authentication.AuthenticationState?.Ip,
            ip => ip.LoginAttempts,
            (ip, v) => ip.LoginAttempts = v,
            ip => ip.LoginLockedOutDate,
            (ip, v) => ip.LoginLockedOutDate = v,
            maxAttempts: config.LoginMaxAttempt,
            lockoutDuration: TimeSpan.FromMinutes(config.LoginMaxAttemptTimeout),
            success: true,
            ct: ct);
    public async Task<bool> AfterUnSuccesfullLoginAsync(CancellationToken ct)
        => await CheckAndUpdateIpLockout(
            authentication.AuthenticationState?.Ip,
            ip => ip.LoginAttempts,
            (ip, v) => ip.LoginAttempts = v,
            ip => ip.LoginLockedOutDate,
            (ip, v) => ip.LoginLockedOutDate = v,
            maxAttempts: config.LoginMaxAttempt,
            lockoutDuration: TimeSpan.FromMinutes(config.LoginMaxAttemptTimeout),
            success: false,
            ct: ct);
    public async Task<bool> BeforeRegisterAsync(CancellationToken ct)
        => await CheckAndUpdateIpLockout(
            authentication.AuthenticationState?.Ip,
            ip => ip.RegisterCount,
            (ip, v) => ip.RegisterCount = v,
            ip => ip.RegisterLockedOutDate,
            (ip, v) => ip.RegisterLockedOutDate = v,
            maxAttempts: config.RegisterMaxAttempt,
            lockoutDuration: TimeSpan.FromHours(config.RegisterMaxAttemptTimeout),
            success: false,
            ct: ct);
    public async Task<bool> BeforeForgetPasswordAsync(CancellationToken ct)
        => await CheckAndUpdateIpLockout(
            authentication.AuthenticationState?.Ip,
            ip => ip.ForgetPasswordAttempts,
            (ip, v) => ip.ForgetPasswordAttempts = v,
            ip => ip.ForgetPasswordLockedOutDate,
            (ip, v) => ip.ForgetPasswordLockedOutDate = v,
            maxAttempts: config.ForgetPasswordMaxAttempt,
            lockoutDuration: TimeSpan.FromHours(config.ForgetPasswordMaxAttemptTimeout),
            success: false,
            ct: ct);
    public async Task<bool> AfterSuccesfullChangePasswordAsync(CancellationToken ct)
        => await CheckAndUpdateIpLockout(
            authentication.AuthenticationState?.Ip,
            ip => ip.ChangePasswordAttempts,
            (ip, v) => ip.ChangePasswordAttempts = v,
            ip => ip.ChangePasswordLockedOutDate,
            (ip, v) => ip.ChangePasswordLockedOutDate = v,
            maxAttempts: config.ChangePasswordMaxAttempt,
            lockoutDuration: TimeSpan.FromMinutes(config.ChangePasswordMaxAttemptTimeout),
            success: true,
            ct: ct);
    public async Task<bool> AfterUnSuccesfullChangePasswordAsync(CancellationToken ct)
        => await CheckAndUpdateIpLockout(
            authentication.AuthenticationState?.Ip,
            ip => ip.ChangePasswordAttempts,
            (ip, v) => ip.ChangePasswordAttempts = v,
            ip => ip.ChangePasswordLockedOutDate,
            (ip, v) => ip.ChangePasswordLockedOutDate = v,
            maxAttempts: config.ChangePasswordMaxAttempt,
            lockoutDuration: TimeSpan.FromMinutes(config.ChangePasswordMaxAttemptTimeout),
            success: false,
            ct: ct);

    private async Task<bool> CheckAndUpdateIpLockout(
        Ip<TUser>? ip,
        Func<Ip<TUser>, int> getAttempts,
        Action<Ip<TUser>, int> setAttempts,
        Func<Ip<TUser>, DateTimeOffset?> getLockedOutDate,
        Action<Ip<TUser>, DateTimeOffset?> setLockedOutDate,
        int maxAttempts,
        TimeSpan lockoutDuration,
        bool success,
        CancellationToken ct)
    {
        if (ip == null)
            throw new Exception("Initialize the ServerAuthenticationService first please");

        var now = timeProvider.GetUtcNow();
        var db = await dbFactory.CreateDbContextAsync(ct);
        ip = db.Ips.FirstOrDefault(a => a.Id == ip.Id);
        if (ip == null) return false;

        // Check lockout
        var lockedOutUntil = getLockedOutDate(ip);
        if (lockedOutUntil.HasValue && lockedOutUntil.Value > now)
            return false; // Locked out

        if (success)
        {
            // Reset attempts and lockout
            setAttempts(ip, 0);
            setLockedOutDate(ip, null);
        }
        else
        {
            var attempts = getAttempts(ip) + 1;
            setAttempts(ip, attempts);

            if (attempts >= maxAttempts)
            {
                setLockedOutDate(ip, now.Add(lockoutDuration));
            }
        }


        await db.SaveChangesAsync(ct);
        return true;
    }
}