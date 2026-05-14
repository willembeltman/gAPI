namespace gAPI.Core.Server.Storage.StorageServer.Dtos.Responses;


public class AppendResponse : Response
{
    public long Length { get; set; }
    public string? EntityFileName { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public string? Url { get; set; }
}