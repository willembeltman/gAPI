namespace gAPI.Core.Server.Storage.StorageServer.Dtos.Requests;


public class DownloadRequest : Request
{
    public string Token { get; set; } = string.Empty;
}