using System.Text.Json;

namespace gAPI.Fabric.Models;

public class FabricConfig
{
    public int Port { get; set; } = 9494;

    public async static Task<FabricConfig> LoadAsync()
    {
        if (!File.Exists("config.json"))
        {
            var newConfig = new FabricConfig();
            await using var newStream = File.Create("config.json");
            await JsonSerializer.SerializeAsync(newStream, newConfig, new JsonSerializerOptions { WriteIndented = true });
            return newConfig;
        }
        var stream = File.OpenRead("config.json");
        if (stream == null)
            return new FabricConfig();
        var config = await JsonSerializer.DeserializeAsync<FabricConfig>(stream);
        return config ?? new FabricConfig();
    }
}