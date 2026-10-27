namespace gAPI.Core.Server.Storage.StorageServer.Dtos.Responses;

public class OpenResponse(string mimeType, string fileName, Stream stream)
{
    public string MimeType { get; } = mimeType;
    public string FileName { get; } = fileName;
    public Stream Stream { get; } = stream;
}