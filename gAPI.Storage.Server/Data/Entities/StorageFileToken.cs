using System.ComponentModel.DataAnnotations;

namespace gAPI.Storage.Server.Data.Entities;

public class StorageFileToken
{
    [Key]
    public long Id { set; get; }
    public long StorageFileId { set; get; }

    public DateTime DateTime { set; get; } = DateTime.Now;
    [StringLength(256)]
    public string Token { get; set; } = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
}