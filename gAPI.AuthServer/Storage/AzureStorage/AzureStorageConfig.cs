namespace gAPI.Storage.AzureStorage;

// Configuration class
public class AzureStorageConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
}