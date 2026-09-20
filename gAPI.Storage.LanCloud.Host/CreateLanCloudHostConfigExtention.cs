using gAPI.Storage.LanCloud.Shared.Models;
using Microsoft.Extensions.Configuration;

namespace gAPI.Storage.LanCloud.Host;

public static class CreateLanCloudHostConfigExtention
{
    public static LanCloudHostConfig CreateLanCloudHostConfig(this IConfigurationManager m)
    {
        var config = new LanCloudHostConfig(
            m["ApiBackendUrl"] ?? "",
            m["WssBackendUrl"] ?? "",
            m.GetSection("LocalShares").Get<LocalShare[]>() ?? []);
        return config;
    }
}