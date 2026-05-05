using gAPI.Core.Server.Storage.StorageServer.Dtos;
using System.Text;

namespace gAPI.Storage.Server.Config;

public class LocalStorageServerConfig
{
    public string SuperSecretKey { get; set; } = string.Empty;
    public Credential Credentials { get; set; } = new Credential();

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