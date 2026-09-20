using gAPI.Core.Server.Config;
using gAPI.Storage.LanCloud.Shared.Models;

namespace gAPI.Storage.LanCloud.Api;

public record LanCloudApiConfig(
    LocalShare LocalShare,
    string? CertificateFilename = null,
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
    int LongHoursAgo = -72)
    : ServerConfig(
        FrontendUrl, 
        DefaultConnectionString, StorageConnectionString, FabricConnectionString,
        UseMemoryDatabase, 
        LoginMaxAttempt, LoginMaxAttemptTimeout,
        RegisterMaxAttempt, RegisterMaxAttemptTimeout,
        ForgetPasswordMaxAttempt, ForgetPasswordMaxAttemptTimeout,
        ChangePasswordMaxAttempt, ChangePasswordMaxAttemptTimeout,
        ShortHoursAgo, LongHoursAgo);