namespace gAPI.Dtos;

public class ServerConfig
{
    public ServerConfig(
        string frontendUrl, bool useMemoryDatabase, string defaultConnectionString, string storageConnectionString, string? fabricConnectionString,
        int? loginMaxAttempt, long? loginMaxAttemptTimeout,
        int? registerMaxAttempt, long? registerMaxAttemptTimeout,
        int? forgetPasswordMaxAttempt, long? forgetPasswordMaxAttemptTimeout,
        int? changePasswordMaxAttempt, long? changePasswordMaxAttemptTimeout,
        int? shortHoursAgo, int? longHoursAgo)
    {
        FrontendUrl = frontendUrl;
        UseMemoryDatabase = useMemoryDatabase;
        DefaultConnectionString = defaultConnectionString;
        StorageConnectionString = storageConnectionString;
        FabricConnectionString = fabricConnectionString;

        LoginMaxAttempt = loginMaxAttempt ?? 5;
        LoginMaxAttemptTimeout = loginMaxAttemptTimeout ?? 15;
        RegisterMaxAttempt = registerMaxAttempt ?? 5;
        RegisterMaxAttemptTimeout = registerMaxAttemptTimeout ?? 24 * 7 * 52;
        ForgetPasswordMaxAttempt = forgetPasswordMaxAttempt ?? 5;
        ForgetPasswordMaxAttemptTimeout = forgetPasswordMaxAttemptTimeout ?? 24;
        ChangePasswordMaxAttempt = changePasswordMaxAttempt ?? 5;
        ChangePasswordMaxAttemptTimeout = changePasswordMaxAttemptTimeout ?? 24;
        ShortHoursAgo = shortHoursAgo ?? -1;
        LongHoursAgo = longHoursAgo ?? -72;
    }

    public string FrontendUrl { get; }
    public bool UseMemoryDatabase { get; }
    public string DefaultConnectionString { get; }
    public string StorageConnectionString { get; }
    public string? FabricConnectionString { get; }

    public int LoginMaxAttempt { get; }
    public long LoginMaxAttemptTimeout { get; }

    public int RegisterMaxAttempt { get; }
    public long RegisterMaxAttemptTimeout { get; }

    public int ForgetPasswordMaxAttempt { get; }
    public long ForgetPasswordMaxAttemptTimeout { get; }

    public int ChangePasswordMaxAttempt { get; }
    public long ChangePasswordMaxAttemptTimeout { get; }

    public int ShortHoursAgo { get; }
    public int LongHoursAgo { get; }
}
