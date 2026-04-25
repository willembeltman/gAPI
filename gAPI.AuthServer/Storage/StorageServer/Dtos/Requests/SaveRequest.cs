namespace gAPI.Storage.StorageServer.Dtos.Requests;


public class SaveRequest : Request
{
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public bool AllowOverwrite { get; set; } = false;
}