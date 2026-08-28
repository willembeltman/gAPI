using gAPI.Core.Server.Config;
using Microsoft.Extensions.Configuration;

namespace gAPI.Core.Server.Extensions;

public static class CreateServerConfigExtension
{
    public static ServerConfig CreateServerConfig(this IConfigurationManager m)
    {
        var config = new ServerConfig(
            m["FrontendUrl"] ?? "",
            m.GetConnectionString("DefaultConnection") ?? throw new Exception("no default db connectionstring?"),
            m.GetConnectionString("StorageConnection") ?? throw new Exception("no storage connectionstring?"),
            m.GetConnectionString("FabricConnection"),
            m["UseMemoryDatabase"]?.ToLower() == "true",
            m.Properties.ContainsKey("LoginMaxAttempt") ? (int.TryParse(m.Properties["LoginMaxAttempt"].ToString(), out var LoginMaxAttempt) ? LoginMaxAttempt : 5) : 5,
            m.Properties.ContainsKey("LoginMaxAttemptTimeout") ? (long.TryParse(m.Properties["LoginMaxAttemptTimeout"].ToString(), out var LoginMaxAttemptTimeout) ? LoginMaxAttemptTimeout : 15) : 15,
            m.Properties.ContainsKey("RegisterMaxAttempt") ? (int.TryParse(m.Properties["RegisterMaxAttempt"].ToString(), out var RegisterMaxAttempt) ? RegisterMaxAttempt : 5) : 5,
            m.Properties.ContainsKey("RegisterMaxAttemptTimeout") ? (long.TryParse(m.Properties["RegisterMaxAttemptTimeout"].ToString(), out var RegisterMaxAttemptTimeout) ? RegisterMaxAttemptTimeout : 24 * 7 * 52) : 24 * 7 * 52,
            m.Properties.ContainsKey("ForgetPasswordMaxAttempt") ? (int.TryParse(m.Properties["ForgetPasswordMaxAttempt"].ToString(), out var ForgetPasswordMaxAttempt) ? ForgetPasswordMaxAttempt : 5) : 5,
            m.Properties.ContainsKey("ForgetPasswordMaxAttemptTimeout") ? (long.TryParse(m.Properties["ForgetPasswordMaxAttemptTimeout"].ToString(), out var ForgetPasswordMaxAttemptTimeout) ? ForgetPasswordMaxAttemptTimeout : 24) : 24,
            m.Properties.ContainsKey("ChangePasswordMaxAttempt") ? (int.TryParse(m.Properties["ChangePasswordMaxAttempt"].ToString(), out var ChangePasswordMaxAttempt) ? ChangePasswordMaxAttempt : 5) : 5,
            m.Properties.ContainsKey("ChangePasswordMaxAttemptTimeout") ? (long.TryParse(m.Properties["ChangePasswordMaxAttemptTimeout"].ToString(), out var ChangePasswordMaxAttemptTimeout) ? ChangePasswordMaxAttemptTimeout : 24) : 24,
            m.Properties.ContainsKey("ShortHoursAgo") ? (int.TryParse(m.Properties["ShortHoursAgo"].ToString(), out var ShortHoursAgo) ? ShortHoursAgo : -1) : -1,
            m.Properties.ContainsKey("LongHoursAgo") ? (int.TryParse(m.Properties["LongHoursAgo"].ToString(), out var LongHoursAgo) ? LongHoursAgo : -72) : -72);

        return config;
    }
}
