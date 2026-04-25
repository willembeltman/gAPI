using System.Text.Json.Serialization;

namespace gAPI.Storage.StorageServer.Dtos.Responses;

public class GetStorageFileInfoResponse : Response
{
    public string? BaseUrl { get; set; }
    public string? BaseFolder { get; set; }
    public string? Folder { get; set; }
    public string? FileName { get; set; }
    public string? Token { get; set; }
    public string? MimeType { get; set; }
    [JsonIgnore]
    public string? Url
    {
        get
        {
            if (string.IsNullOrWhiteSpace(BaseUrl) ||
                string.IsNullOrWhiteSpace(BaseFolder) ||
                string.IsNullOrWhiteSpace(Folder) ||
                string.IsNullOrWhiteSpace(FileName) ||
                string.IsNullOrWhiteSpace(Token))
            {
                return null;
            }

            var uri = new Uri(BaseUrl);
            if (uri.Port <= 0 || uri.Port == 80)
            {
                return $"{uri.Scheme}://{uri.Host}/{BaseFolder}/{Uri.EscapeDataString(Folder)}/{Uri.EscapeDataString(FileName)}?token={Token}";
            }
            return $"{uri.Scheme}://{uri.Host}:{uri.Port}/{BaseFolder}/{Uri.EscapeDataString(Folder)}/{Uri.EscapeDataString(FileName)}?token={Token}";
        }
    }

    public string? Sha256 { get; set; }
    public long? Length { get; set; }
    public string? EntityFileName { get; set; }
}