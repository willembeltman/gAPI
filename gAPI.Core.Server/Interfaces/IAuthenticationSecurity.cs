namespace gAPI.Core.Interfaces;

public interface IAuthenticationSecurity
{
    Task<bool> AfterSuccesfullChangePasswordAsync(CancellationToken ct);
    Task<bool> AfterSuccesfullLoginAsync(CancellationToken ct);
    Task<bool> AfterUnSuccesfullChangePasswordAsync(CancellationToken ct);
    Task<bool> AfterUnSuccesfullLoginAsync(CancellationToken ct);
    Task<bool> BeforeChangePasswordAsync(CancellationToken ct);
    Task<bool> BeforeForgetPasswordAsync(CancellationToken ct);
    Task<bool> BeforeLoginAsync(CancellationToken ct);
    Task<bool> BeforeRegisterAsync(CancellationToken ct);
}