namespace gAPI.Core.Server.Storage.StorageServer.Dtos.Responses;


public class AppendResponse : Response
{
    public string? Url { get; set; }
    public StorageFileInfo? FileInfo { get; set; }
}