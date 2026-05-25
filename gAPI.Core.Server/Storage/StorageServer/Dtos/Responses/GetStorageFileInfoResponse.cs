using System.Text.Json.Serialization;

namespace gAPI.Core.Server.Storage.StorageServer.Dtos.Responses;

public class GetStorageFileInfoResponse : Response
{
    public string? BaseUrl { get; set; }
    public string? BaseFolder { get; set; }
    public StorageFileInfo? FileInfo { get; set; }

    public string? Token { get; set; }

    [JsonIgnore]
    public string? Url
    {
        get
        {
            if (FileInfo == null ||
                string.IsNullOrWhiteSpace(BaseUrl) ||
                string.IsNullOrWhiteSpace(BaseFolder) ||
                string.IsNullOrWhiteSpace(FileInfo.Key) ||
                string.IsNullOrWhiteSpace(FileInfo.Key) ||
                string.IsNullOrWhiteSpace(Token))
            {
                return null;
            }

            var uri = new Uri(BaseUrl);
            if (uri.Port <= 0 || uri.Port == 80)
            {
                return $"{uri.Scheme}://{uri.Host}/{BaseFolder}/{Uri.EscapeDataString(FileInfo.Key)}?token={Token}";
            }
            return $"{uri.Scheme}://{uri.Host}:{uri.Port}/{BaseFolder}/{Uri.EscapeDataString(FileInfo.Key)}?token={Token}";
        }
    }
}