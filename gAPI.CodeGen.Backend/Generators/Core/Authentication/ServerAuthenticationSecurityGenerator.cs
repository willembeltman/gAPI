using gAPI.CodeGen.Backend.Models;

namespace gAPI.CodeGen.Backend.Generators.Core.Authentication;

public class ServerAuthenticationSecurityGenerator : BaseGenerator
{
    public ServerAuthenticationSecurityGenerator(
        BackendGenerator context)
    {
        Directory = context.Config.Core_AuthenticationDirectory;
        Namespace = context.Config.Core_AuthenticationNamespace;

        Context = context;

        Name = "ServerAuthenticationSecurity";
        FileName = $"{Name}.cs";
    }

    public BackendGenerator Context { get; }

    public SharedReference IServerAuthenticationService => Context.IServerAuthenticationService;
    public SharedReference IServerAuthenticationSecurity => Context.IServerAuthenticationSecurity;
    public SharedReference ApplicationDbContext => Context.DbContext;
    public SharedReference ServerConfig => Context.SharedReferences.ServerConfig;
    public SharedReference Ip => Context.Ip;

    public void GenerateCode()
    {
        Reg(IServerAuthenticationService);
        Reg(IServerAuthenticationSecurity);
        Reg(ApplicationDbContext);
        Reg(ServerConfig);
        Reg(Ip);

        Code = $@"{GetNamespacesCode()}
namespace {Namespace};

public class {Name}(
    {ApplicationDbContext} db,
    {IServerAuthenticationService} authentication,
    TimeProvider timeProvider,
    {ServerConfig} config) 
    : {IServerAuthenticationSecurity}
{{
    public async Task<bool> BeforeLoginAsync(CancellationToken ct)
    {{
        if (authentication.State == null || authentication.State.DbIp == null)
            throw new Exception(""Initialize the ServerAuthenticationService first please"");

        var now = timeProvider.GetUtcNow();
        var lockedOutUntil = authentication.State.DbIp.LoginLockedOutDate;

        if (lockedOutUntil.HasValue && lockedOutUntil.Value > now)
            return false; // Geblokkeerd

        return true; // Niet geblokkeerd, mag proberen
    }}
    public async Task<bool> BeforeChangePasswordAsync(CancellationToken ct)
    {{
        if (authentication.State == null || authentication.State.DbIp == null)
            throw new Exception(""Initialize the ServerAuthenticationService first please"");

        var now = timeProvider.GetUtcNow();
        var lockedOutUntil = authentication.State.DbIp.ChangePasswordLockedOutDate;

        if (lockedOutUntil.HasValue && lockedOutUntil.Value > now)
            return false; // Geblokkeerd

        return true; // Niet geblokkeerd, mag proberen
    }}
    public async Task<bool> AfterSuccesfullLoginAsync(CancellationToken ct) =>
        await CheckAndUpdateIpLockout(
            authentication.State?.DbIp,
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
            authentication.State?.DbIp,
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
            authentication.State?.DbIp,
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
            authentication.State?.DbIp,
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
            authentication.State?.DbIp,
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
            authentication.State?.DbIp,
            ip => ip.ChangePasswordAttempts,
            (ip, v) => ip.ChangePasswordAttempts = v,
            ip => ip.ChangePasswordLockedOutDate,
            (ip, v) => ip.ChangePasswordLockedOutDate = v,
            maxAttempts: config.ChangePasswordMaxAttempt,
            lockoutDuration: TimeSpan.FromMinutes(config.ChangePasswordMaxAttemptTimeout),
            success: false,
            ct: ct);

    private async Task<bool> CheckAndUpdateIpLockout(
        {Ip}? ip,
        Func<{Ip}, int> getAttempts,
        Action<{Ip}, int> setAttempts,
        Func<{Ip}, DateTimeOffset?> getLockedOutDate,
        Action<{Ip}, DateTimeOffset?> setLockedOutDate,
        int maxAttempts,
        TimeSpan lockoutDuration,
        bool success,
        CancellationToken ct)
    {{
        if (ip == null)
            throw new Exception(""Initialize the ServerAuthenticationService first please"");

        var now = timeProvider.GetUtcNow();

        // Check lockout
        var lockedOutUntil = getLockedOutDate(ip);
        if (lockedOutUntil.HasValue && lockedOutUntil.Value > now)
            return false; // Locked out

        if (success)
        {{
            // Reset attempts and lockout
            setAttempts(ip, 0);
            setLockedOutDate(ip, null);
        }}
        else
        {{
            var attempts = getAttempts(ip) + 1;
            setAttempts(ip, attempts);

            if (attempts >= maxAttempts)
            {{
                setLockedOutDate(ip, now.Add(lockoutDuration));
            }}
        }}

        await db.SaveChangesAsync(ct);
        return true;
    }}
}}";
        Save(false);
    }
}