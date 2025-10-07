using System.ComponentModel.DataAnnotations;

namespace gAPI.Storage.Server.Data.Entities
{
    public class StorageFileToken
    {
        [Key]
        public long Id { set; get; }
        public long StorageFileId { set; get; }

        public DateTime DateTime { set; get; } = DateTime.Now;
        public string Token { get; set; } = Guid.NewGuid().ToString().Replace("-", "");

        //public virtual ILazy<StorageFile> StorageFile { set; get; }
    }
}