#nullable enable
namespace gAPI.Storage.Mock;

// Configuration
public class MockStorageConfig
{
    public string? BaseUrl { get; set; } = "https://mock-storage.local";
    public int SimulateLatencyMs { get; set; } = 50; // Simuleer realistische latency
    public bool LogOperations { get; set; } = true;
}