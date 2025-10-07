using System.ComponentModel.DataAnnotations;


namespace gAPI.Storage.Server.Data.Entities
{
    public class StorageFile
    {
        [Key]
        public long Id { set; get; }
        public long StorageFolderId { set; get; }

        public string? EntityFileName { set; get; }
        public string? EntityId { get; set; }
        public string? FileName { get; set; }
        public long Length { set; get; }
        public string? MimeType { set; get; }
        public string? Sha256 { get; set; }
    }
}