namespace gAPI.Core.Server.Storage.StorageServer.Dtos.Requests;


public class AppendRequest : Request
{
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public bool AllowCreate { get; set; } = false;
}