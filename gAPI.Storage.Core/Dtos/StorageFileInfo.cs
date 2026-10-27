using gAPI.Core.Attributes;

namespace gAPI.Core.Server.Storage;

public class StorageFileInfo
{
    [IsHidden]
    public string Key { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long? Length { get; set; }
}
