using gAPI.Storage.StorageServer.Dtos;

#nullable enable
namespace gAPI.Storage.StorageServer
{
    public class StorageServerConfig
    {
        public string? ServerUrl { get; set; }
        public Credential Credential { get; set; } = new Credential();
    }
}