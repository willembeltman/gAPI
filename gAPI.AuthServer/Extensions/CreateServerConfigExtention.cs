using gAPI.Dtos;
using Microsoft.Extensions.Configuration;

namespace gAPI.Extensions;

public static class CreateServerConfigExtension
{
    public static ServerConfig CreateServerConfig(this IConfigurationManager m)
    {
        var config = new ServerConfig(
            m["FrontendUrl"] ?? "",
            m["UseMemoryDatabase"]?.ToLower() == "true",
            m.GetConnectionString("DefaultConnection") ?? throw new Exception("no default db connectionstring?"),
            m.GetConnectionString("StorageConnection") ?? throw new Exception("no storage connectionstring?"),
            m.GetConnectionString("FabricConnection"),
            m.Properties.ContainsKey("LoginMaxAttempt") ? (int.TryParse(m.Properties["LoginMaxAttempt"].ToString(), out var LoginMaxAttempt) ? LoginMaxAttempt : null) : null,
            m.Properties.ContainsKey("LoginMaxAttemptTimeout") ? (long.TryParse(m.Properties["LoginMaxAttemptTimeout"].ToString(), out var LoginMaxAttemptTimeout) ? LoginMaxAttemptTimeout : null) : null,
            m.Properties.ContainsKey("RegisterMaxAttempt") ? (int.TryParse(m.Properties["RegisterMaxAttempt"].ToString(), out var RegisterMaxAttempt) ? RegisterMaxAttempt : null) : null,
            m.Properties.ContainsKey("RegisterMaxAttemptTimeout") ? (long.TryParse(m.Properties["RegisterMaxAttemptTimeout"].ToString(), out var RegisterMaxAttemptTimeout) ? RegisterMaxAttemptTimeout : null) : null,
            m.Properties.ContainsKey("ForgetPasswordMaxAttempt") ? (int.TryParse(m.Properties["ForgetPasswordMaxAttempt"].ToString(), out var ForgetPasswordMaxAttempt) ? ForgetPasswordMaxAttempt : null) : null,
            m.Properties.ContainsKey("ForgetPasswordMaxAttemptTimeout") ? (long.TryParse(m.Properties["ForgetPasswordMaxAttemptTimeout"].ToString(), out var ForgetPasswordMaxAttemptTimeout) ? ForgetPasswordMaxAttemptTimeout : null) : null,
            m.Properties.ContainsKey("ChangePasswordMaxAttempt") ? (int.TryParse(m.Properties["ChangePasswordMaxAttempt"].ToString(), out var ChangePasswordMaxAttempt) ? ChangePasswordMaxAttempt : null) : null,
            m.Properties.ContainsKey("ChangePasswordMaxAttemptTimeout") ? (long.TryParse(m.Properties["ChangePasswordMaxAttemptTimeout"].ToString(), out var ChangePasswordMaxAttemptTimeout) ? ChangePasswordMaxAttemptTimeout : null) : null,
            m.Properties.ContainsKey("ShortHoursAgo") ? (int.TryParse(m.Properties["ShortHoursAgo"].ToString(), out var ShortHoursAgo) ? ShortHoursAgo : null) : null,
            m.Properties.ContainsKey("LongHoursAgo") ? (int.TryParse(m.Properties["LongHoursAgo"].ToString(), out var LongHoursAgo) ? LongHoursAgo : null) : null);

        return config;
    }
}
