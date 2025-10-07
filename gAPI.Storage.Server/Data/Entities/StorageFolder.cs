using System.ComponentModel.DataAnnotations;


namespace gAPI.Storage.Server.Data.Entities
{
    public class StorageFolder
    {
        [Key]
        public long Id { set; get; }
        public string? Name { set; get; }
    }
}