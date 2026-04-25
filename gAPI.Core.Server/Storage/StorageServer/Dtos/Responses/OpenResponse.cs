using System.IO;

namespace gAPI.Storage.StorageServer.Dtos.Responses;

public class OpenResponse
{
    public string MimeType { get; }
    public string FileName { get; }
    public Stream Stream { get; }

    public OpenResponse(string mimeType, string fileName, Stream stream)
    {
        MimeType = mimeType;
        FileName = fileName;
        Stream = stream;
    }
}