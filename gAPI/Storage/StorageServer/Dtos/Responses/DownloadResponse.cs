using System.IO;

namespace gAPI.Storage.StorageServer.Dtos.Responses;


public class DownloadResponse : Response
{
    public DownloadResponse()
    {
    }
    public DownloadResponse(string mimeType, string fileName, FileStream stream)
    {
        MimeType = mimeType;
        FileName = fileName;
        Stream = stream;
    }

    public string? MimeType { get; set; }
    public string? FileName { get; set; }
    public Stream? Stream { get; set; }
}