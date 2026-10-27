using System.Text;

namespace gAPI.Storage.Server;

public record LocalStorageServerConfig(
    string SuperSecretKey,
    string BaseUrl,
    int? MaxPackageSize = null,
    string? BusConnectionString = null)
{
    private byte[]? _SuperSecretKeyArray { get; set; }
    public byte[] SuperSecretKeyArray
    {
        get
        {
            if (_SuperSecretKeyArray == null)
                _SuperSecretKeyArray = Encoding.ASCII.GetBytes(SuperSecretKey);
            return _SuperSecretKeyArray;
        }
    }
}