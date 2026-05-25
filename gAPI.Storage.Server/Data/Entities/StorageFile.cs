using System.ComponentModel.DataAnnotations;


namespace gAPI.Storage.Server.Data.Entities;

public class StorageFile
{
    [Key]
    public long Id { set; get; }
    public long StorageFolderId { set; get; }

    public string Key { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long Length { set; get; }
    public string MimeType { set; get; } = string.Empty;
}