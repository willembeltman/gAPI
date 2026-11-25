using gAPI.Core.Server.Config;
using Microsoft.Extensions.Configuration;

namespace gAPI.Core.Server.Extensions;

public static class CreatePlaneConfigExtension
{
    public static BackPlaneConfig CreatePlaneConfig(this IConfiguration m) // Veranderd naar IConfiguration voor bredere ondersteuning
    {
        // Gebruik .GetValue<int?> of int.TryParse op de stringwaarde
        var maxPackageSizeStr = m.GetSection("MaxPackageSize").Value;
        int? maxPackageSize = int.TryParse(maxPackageSizeStr, out var parsedSize) ? parsedSize : null;

        // Aspire vult dit nu automatisch in omdat de component "BackPlane" heet in de AppHost
        var connectionString = m.GetConnectionString("BackPlaneConnection")
            ?? m.GetConnectionString("BackPlane")
            ?? m.GetConnectionString("PlaneConnection")
            ?? m.GetConnectionString("Fabric")
            ?? m.GetConnectionString("fabric")
            ?? throw new Exception("BackPlane connectionstring not found in config");

        return new BackPlaneConfig(
            maxPackageSize,
            connectionString);
    }
}
