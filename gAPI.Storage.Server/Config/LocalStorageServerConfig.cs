using gAPI.Storage.StorageServer.Dtos;
using System.Text;

namespace gAPI.Storage.Server.Config
{
    public class LocalStorageServerConfig
    {
        public string? SuperSecretKey { get; set; }
        public Credential[]? Credentials { get; set; }

        private byte[]? _SuperSecretKeyArray { get; set; }
        public byte[]? SuperSecretKeyArray
        {
            get
            {
                if (_SuperSecretKeyArray == null && SuperSecretKey != null)
                    _SuperSecretKeyArray = Encoding.ASCII.GetBytes(SuperSecretKey);
                return _SuperSecretKeyArray;
            }
        }
    }
}