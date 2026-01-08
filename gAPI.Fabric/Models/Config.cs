using System.Text.Json;

namespace gAPI.FabricNode.Models;

public class Config
{
    public int Port { get; set; } = 9494;

    public async static Task<Config> LoadAsync()
    {
        if (!File.Exists("config.json"))
        {
            var newConfig = new Config();
            await using var newStream = File.Create("config.json");
            await JsonSerializer.SerializeAsync(newStream, newConfig, new JsonSerializerOptions { WriteIndented = true });
            return newConfig;
        }
        var stream = File.OpenRead("config.json");
        if (stream == null)
            return new Config();
        var config = await JsonSerializer.DeserializeAsync<Config>(stream);
        return config ?? new Config();
    }
}