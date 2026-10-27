using gAPI.Storage.Client;
using Microsoft.Extensions.Configuration;

namespace gAPI.Storage.Client.Extensions;

public static class CreateStorageConfigExtension
{
    public static StorageConfig CreateStorageConfig(this IConfigurationManager m)
    {
        var config = new StorageConfig(
            int.TryParse(m["MaxPackageSize"], out var maxPackageSize) ? maxPackageSize : null,
            m.GetConnectionString("StorageConnection"));
        return config;
    }
}
