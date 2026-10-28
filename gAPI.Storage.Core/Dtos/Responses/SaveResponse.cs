using gAPI.Storage.Core.Dtos;

namespace gAPI.Storage.Core.Dtos.Responses;


public class SaveResponse : Response
{
    public string? Url { get; set; }
    public StorageFileInfo? FileInfo { get; set; }
}