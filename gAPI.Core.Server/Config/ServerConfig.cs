namespace gAPI.Core.Server.Config;

public record ServerConfig(
    string? FrontendUrl = null,
    string? DefaultConnectionString = null,
    string? StorageConnectionString = null,
    string? FabricConnectionString = null,
    bool UseMemoryDatabase = false,
    int LoginMaxAttempt = 5,
    long LoginMaxAttemptTimeout = 15,
    int RegisterMaxAttempt = 5,
    long RegisterMaxAttemptTimeout = 24 * 7 * 52, // 8736
    int ForgetPasswordMaxAttempt = 5,
    long ForgetPasswordMaxAttemptTimeout = 24,
    int ChangePasswordMaxAttempt = 5,
    long ChangePasswordMaxAttemptTimeout = 24,
    int ShortHoursAgo = -1,
    int LongHoursAgo = -72
);