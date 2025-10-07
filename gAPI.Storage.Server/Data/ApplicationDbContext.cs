using gAPI.EntityFrameworkDisk;
using gAPI.Storage.Server.Data.Entities;

#nullable disable

namespace gAPI.Storage.Server.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<StorageFolder> StorageFolders { get; set; }
        public DbSet<StorageFile> StorageFiles { get; set; }
        public DbSet<StorageFileToken> StorageFileTokens { get; set; }
    }
}